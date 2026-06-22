<#
.SYNOPSIS
    OptimFoundation project workflow tool.
    Builds and runs all (or specified) projects under Projects/.

.PARAMETER Project
    Project folder name. Empty = all projects.

.PARAMETER BuildOnly
    Build only, do not run.

.PARAMETER RunOnly
    Run only (assumes already built).

.PARAMETER Config
    Build configuration: Debug (default) or Release.

.EXAMPLE
    .\run.ps1
    .\run.ps1 -Project GlassFactory
    .\run.ps1 -BuildOnly
    .\run.ps1 -Project GlassFactory -Config Release
#>
param(
    [string] $Project = "",
    [switch] $BuildOnly,
    [switch] $RunOnly,
    [string] $Config  = "Debug"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Continue"

$Root        = Split-Path (Split-Path $MyInvocation.MyCommand.Path)   # scripts/ の 上一層
$ProjectsDir = Join-Path $Root "Projects"

# Collect projects
$allProjects = Get-ChildItem -Path $ProjectsDir -Directory -ErrorAction Stop
if ($Project) {
    $allProjects = $allProjects | Where-Object { $_.Name -eq $Project }
    if (-not $allProjects) {
        Write-Host "[ERROR] Project not found: $Project" -ForegroundColor Red
        exit 1
    }
}

$results = [System.Collections.Generic.List[PSObject]]::new()

$mode = if ($BuildOnly) { "BuildOnly" } elseif ($RunOnly) { "RunOnly" } else { "Build+Run" }
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  OptimFoundation Workflow  |  Config=$Config  |  Mode=$mode" -ForegroundColor Cyan
Write-Host "  Projects: $ProjectsDir" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

foreach ($proj in $allProjects) {
    $csproj = Get-ChildItem -Path $proj.FullName -Filter "*.csproj" -ErrorAction SilentlyContinue |
              Select-Object -First 1
    if (-not $csproj) {
        Write-Host "[SKIP] $($proj.Name) - no .csproj found" -ForegroundColor Yellow
        continue
    }

    # Skip non-executable projects (source generators / analyzers / class libraries).
    # Only projects with <OutputType>Exe</OutputType> are buildable+runnable here.
    $csprojText = Get-Content $csproj.FullName -Raw
    if ($csprojText -notmatch '(?i)<OutputType>\s*Exe\s*</OutputType>') {
        Write-Host "[SKIP] $($proj.Name) - not an executable project (no OutputType=Exe)" -ForegroundColor Yellow
        continue
    }

    $row = [PSCustomObject]@{
        Project = $proj.Name
        Build   = "-"
        Run     = "-"
        TimeSec = "-"
        Note    = ""
    }

    # ── BUILD ────────────────────────────────────────────────────────────────
    if (-not $RunOnly) {
        Write-Host "[BUILD] $($proj.Name)" -ForegroundColor Cyan
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        $buildOut = dotnet build $csproj.FullName -c $Config --nologo 2>&1
        $sw.Stop()
        $exitCode = $LASTEXITCODE

        $errLines  = @($buildOut | Where-Object { $_ -match " error CS" })
        $warnLines = @($buildOut | Where-Object { $_ -match " warning CS" })

        if ($exitCode -eq 0) {
            $row.Build = "OK"
            $elapsed = [int]$sw.Elapsed.TotalSeconds
            Write-Host "       OK  warnings=$($warnLines.Count)  (${elapsed}s)" -ForegroundColor Green
        } else {
            $row.Build = "FAIL"
            $errMsg = ($errLines | Select-Object -First 2 | ForEach-Object {
                $_ -replace "^.*error CS\d+: ", ""
            }) -join "; "
            $row.Note = $errMsg
            Write-Host "       FAIL  errors=$($errLines.Count)" -ForegroundColor Red
            $errLines | Select-Object -First 3 | ForEach-Object {
                Write-Host "         $_" -ForegroundColor DarkRed
            }
        }
    }

    # ── RUN ──────────────────────────────────────────────────────────────────
    if ((-not $BuildOnly) -and ($row.Build -ne "FAIL")) {
        Write-Host "[RUN  ] $($proj.Name)" -ForegroundColor Cyan
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        $runOut = dotnet run --project $csproj.FullName -c $Config --no-build 2>&1
        $sw.Stop()
        $exitCode = $LASTEXITCODE

        $elapsed = [int]$sw.Elapsed.TotalSeconds
        $row.TimeSec = "${elapsed}s"

        if ($exitCode -eq 0) {
            $row.Run = "OK"
            Write-Host "       OK  (${elapsed}s)" -ForegroundColor Green
        } else {
            $row.Run = "FAIL"
            Write-Host "       FAIL  (${elapsed}s)" -ForegroundColor Red
            if (-not $row.Note) {
                $row.Note = ($runOut | Select-Object -Last 3) -join " | "
            }
        }

        # Print key result lines (filter CPLEX banners)
        $keyLines = @($runOut | Where-Object {
            $_ -match "==|--|optimal|Obj|profit|OK|FAIL|Build|" -or
            $_ -notmatch "IBM|Cplex|license|Version|Copyright|Academic"
        } | Select-Object -Last 20)

        if ($keyLines) {
            Write-Host "       ---- output ----" -ForegroundColor Gray
            $keyLines | ForEach-Object { Write-Host "       | $_" -ForegroundColor White }
            Write-Host "       ----------------" -ForegroundColor Gray
        }
    }

    $results.Add($row)
    Write-Host ""
}

# ── SUMMARY ──────────────────────────────────────────────────────────────────
Write-Host "============================================================" -ForegroundColor Yellow
Write-Host "  Summary" -ForegroundColor Yellow
Write-Host "============================================================" -ForegroundColor Yellow
$results | Format-Table -AutoSize

$buildOK   = @($results | Where-Object { $_.Build -eq "OK"   }).Count
$buildFail = @($results | Where-Object { $_.Build -eq "FAIL" }).Count
$runOK     = @($results | Where-Object { $_.Run   -eq "OK"   }).Count
$runFail   = @($results | Where-Object { $_.Run   -eq "FAIL" }).Count

Write-Host "  Build : OK=$buildOK  FAIL=$buildFail"
Write-Host "  Run   : OK=$runOK   FAIL=$runFail"
Write-Host ""

# ── CSV export ────────────────────────────────────────────────────────────────
$timestamp  = (Get-Date).ToString("yyyy-MM-dd_HH-mm-ss")
$csvPath    = Join-Path $Root "run_results_${timestamp}.csv"
$results |
    Select-Object Project, Build, Run, TimeSec, Note,
        @{ Name = "Timestamp"; Expression = { $timestamp } } |
    Export-Csv -Path $csvPath -NoTypeInformation -Encoding UTF8
Write-Host "  Summary CSV: $csvPath" -ForegroundColor Cyan
Write-Host ""

if ($buildFail -gt 0) {
    Write-Host "  [!] $buildFail project(s) failed to build." -ForegroundColor Red
    exit 1
}
exit 0
