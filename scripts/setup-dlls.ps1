<#
.SYNOPSIS
    One-shot dlls/ provisioning: after cloning to a fresh machine, place the 6 DLLs
    that build needs. Auto-detects (1) local CPLEX install ILOG.* (2) sibling
    OptimFoundation build output. ASCII-only messages so it runs under Windows
    PowerShell 5.1 without a BOM.

.PARAMETER CplexBin
    CPLEX x64_win64 bin dir. If omitted, tries env CPLEX_STUDIO_DIR* then C:\IBM\ILOG\CPLEX_Studio*.

.PARAMETER FoundationDir
    Sibling OptimFoundation repo root. If omitted, tries ..\OptimFoundation relative to this repo.

.PARAMETER Config
    OptimFoundation build config to pull DLLs from (Release default, falls back to Debug).

.PARAMETER Build
    dotnet build the sibling OptimFoundation first, then copy (use when not built yet).

.EXAMPLE
    powershell -File scripts/setup-dlls.ps1
    powershell -File scripts/setup-dlls.ps1 -Build
    powershell -File scripts/setup-dlls.ps1 -CplexBin "D:\CPLEX\cplex\bin\x64_win64"
#>
param(
    [string] $CplexBin = "",
    [string] $FoundationDir = "",
    [string] $Config = "Release",
    [switch] $Build
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$Root = Split-Path (Split-Path $MyInvocation.MyCommand.Path) # scripts/ parent = repo root
$DllsDir = Join-Path $Root "dlls"
if (-not (Test-Path $DllsDir)) { New-Item -ItemType Directory -Path $DllsDir | Out-Null }

function Say($msg, $color = "White") { Write-Host $msg -ForegroundColor $color }
function CopyIn($src, $name) {
    if ($src -and (Test-Path $src)) {
        Copy-Item $src (Join-Path $DllsDir $name) -Force
        Say "  [OK]   $name  <-  $src" Green
        return $true
    }
    Say "  [MISS] $name  (source not found)" Yellow
    return $false
}

Say "============================================================" Cyan
Say "  setup-dlls  |  repo=$Root" Cyan
Say "============================================================" Cyan

# --- 1. CPLEX ILOG.* ---------------------------------------------------------
Say "`n[1/2] CPLEX commercial DLLs (ILOG.Concert / ILOG.CPLEX)" Cyan
if (-not $CplexBin) {
    $envDir = Get-ChildItem Env: | Where-Object { $_.Name -like "CPLEX_STUDIO_DIR*" } |
              Select-Object -First 1 -ExpandProperty Value -ErrorAction SilentlyContinue
    $cands = @()
    if ($envDir) { $cands += (Join-Path $envDir "cplex\bin\x64_win64") }
    $cands += (Get-ChildItem "C:\IBM\ILOG" -Directory -Filter "CPLEX_Studio*" -ErrorAction SilentlyContinue |
               ForEach-Object { Join-Path $_.FullName "cplex\bin\x64_win64" })
    $CplexBin = $cands | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
}
if ($CplexBin) { Say "  CPLEX bin: $CplexBin" Gray } else { Say "  CPLEX install not found (pass -CplexBin)" Yellow }
CopyIn (Join-Path $CplexBin "ILOG.Concert.dll") "ILOG.Concert.dll" | Out-Null
CopyIn (Join-Path $CplexBin "ILOG.CPLEX.dll") "ILOG.CPLEX.dll" | Out-Null

# --- 2. OptimFoundation.* + NLog ---------------------------------------------
Say "`n[2/2] OptimFoundation framework DLLs + NLog" Cyan
if (-not $FoundationDir) {
    foreach ($p in @("..\OptimFoundation", "..\OptimFoundation\OptimFoundation")) {
        $cand = Join-Path $Root $p
        if (Test-Path $cand) { $FoundationDir = (Resolve-Path $cand).Path; break }
    }
}
if ($FoundationDir) { Say "  Foundation: $FoundationDir" Gray } else { Say "  sibling OptimFoundation not found (pass -FoundationDir)" Yellow }

if ($Build -and $FoundationDir) {
    $sln = Get-ChildItem $FoundationDir -Recurse -Filter "OptimFoundation.sln" -ErrorAction SilentlyContinue |
           Select-Object -First 1
    if ($sln) { Say "  building $($sln.FullName) ($Config)..." Gray; dotnet build $sln.FullName -c $Config --nologo | Out-Null }
}

$fwNames = @("OptimFoundation.Core.dll","OptimFoundation.Cplex.dll","OptimFoundation.Generators.dll","NLog.dll")
foreach ($n in $fwNames) {
    $hit = $null
    if ($FoundationDir) {
        $hit = Get-ChildItem $FoundationDir -Recurse -Filter $n -ErrorAction SilentlyContinue |
               Where-Object { $_.FullName -match "\\bin\\" -and $_.FullName -match "\\$Config\\" } |
               Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if (-not $hit) {
            $hit = Get-ChildItem $FoundationDir -Recurse -Filter $n -ErrorAction SilentlyContinue |
                   Where-Object { $_.FullName -match "\\bin\\" } |
                   Sort-Object LastWriteTime -Descending | Select-Object -First 1
        }
    }
    if ($hit) { CopyIn $hit.FullName $n | Out-Null } else { CopyIn $null $n | Out-Null }
}

# --- verify ------------------------------------------------------------------
$need = @("ILOG.Concert.dll","ILOG.CPLEX.dll","NLog.dll",
          "OptimFoundation.Core.dll","OptimFoundation.Cplex.dll","OptimFoundation.Generators.dll")
$have = $need | Where-Object { Test-Path (Join-Path $DllsDir $_) }
$missing = $need | Where-Object { -not (Test-Path (Join-Path $DllsDir $_)) }

# --- provenance: dlls/VERSION.txt (staleness detection; ASCII-only, no BOM) ----
$commit = "unknown"
if ($FoundationDir) {
    # git repo may sit at the shell dir or one level in (this workspace: inner OptimFoundation\OptimFoundation)
    foreach ($gd in @($FoundationDir, (Join-Path $FoundationDir "OptimFoundation"))) {
        if (Test-Path (Join-Path $gd ".git")) {
            $c = & git -C $gd rev-parse --short HEAD 2>$null
            if ($LASTEXITCODE -eq 0 -and $c) { $commit = "$c".Trim(); break }
        }
    }
}
$vlines = @(
    "# OptimFoundation dlls/ provenance -- auto-written by setup-dlls.ps1, DO NOT edit by hand",
    ("generated:     {0}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss")),
    ("source_commit: {0}" -f $commit),
    ("config:        {0}" -f $Config),
    "--- dll last-write ---"
)
foreach ($n in $need) {
    $p = Join-Path $DllsDir $n
    if (Test-Path $p) { $vlines += ("{0}  {1}" -f (Get-Item $p).LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"), $n) }
}
Set-Content -Path (Join-Path $DllsDir "VERSION.txt") -Value $vlines -Encoding ascii
Say "  wrote dlls/VERSION.txt (commit $commit)" Gray

Say "`n============================================================" Yellow
Say "  ready $($have.Count)/6" Yellow
if ($missing) {
    Say "  missing: $($missing -join ', ')" Red
    Say "  -> follow dlls/README.md, or rerun with -CplexBin / -FoundationDir / -Build" Red
    exit 1
}
Say "  all set -> buildable. At run time CPLEX native runtime must be on PATH (see dlls/README.md)" Green
exit 0
