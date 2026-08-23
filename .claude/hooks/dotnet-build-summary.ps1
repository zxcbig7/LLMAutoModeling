# dotnet-build-summary.ps1 -- PostToolUse(Bash) hook
#
# Purpose: when Bash ran `dotnet build` / `run` / `test`, collapse the long output into a few
#          error/warning lines so the Phase 2 build fix loop does not read a whole build log.
#
# Contract: the hook receives one JSON payload on stdin (tool_input / tool_response).
#           Not a dotnet command, or any parse failure -> exit 0 silently, never disturb the tool call.
#
# ASCII ONLY -- do not put non-ASCII characters in this file.
# Windows PowerShell 5.1 reads a BOM-less .ps1 using the ANSI codepage, so UTF-8 Chinese text is
# corrupted on load (verified on this machine: PS 5.1.26100). Repo policy forbids writing a BOM,
# so the only safe option is to keep this script ASCII and match localized build output by
# diagnostic code (CSxxxx / MSBxxxx) instead of by localized words.

$ErrorActionPreference = 'Stop'

try {
    # PS 5 reads stdin with the OEM codepage; pin UTF-8 before reading or non-ASCII build
    # output breaks the JSON parse (and the catch below would swallow it silently).
    [Console]::InputEncoding = New-Object System.Text.UTF8Encoding $false
    [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding $false

    $raw = [Console]::In.ReadToEnd()
    if ([string]::IsNullOrWhiteSpace($raw)) { exit 0 }

    $payload = $raw | ConvertFrom-Json

    $command = ''
    if ($payload.tool_input -and $payload.tool_input.command) { $command = [string]$payload.tool_input.command }
    if ($command -notmatch 'dotnet\s+(build|run|test)') { exit 0 }

    # tool_response shape varies by host: a plain string, or an object carrying stdout/stderr.
    $output = ''
    $response = $payload.tool_response
    if ($null -ne $response) {
        if ($response -is [string]) {
            $output = $response
        }
        else {
            foreach ($field in @('stdout', 'stderr', 'output', 'content')) {
                if ($response.PSObject.Properties.Name -contains $field) {
                    $output += [string]$response.$field + "`n"
                }
            }
        }
    }
    if ([string]::IsNullOrWhiteSpace($output)) { exit 0 }

    $lines = $output -split "`r?`n"

    $errorPattern = ':\s*error\s+[A-Za-z]+\d+'
    $warnPattern = ':\s*warning\s+[A-Za-z]+\d+'

    $errorLines = @($lines | Where-Object { $_ -match $errorPattern })
    $warnLines = @($lines | Where-Object { $_ -match $warnPattern })

    if ($errorLines.Count -eq 0 -and $warnLines.Count -eq 0) { exit 0 }

    $summary = New-Object System.Collections.Generic.List[string]
    $summary.Add("[build-summary] errors=$($errorLines.Count) warnings=$($warnLines.Count)")

    # One line per distinct diagnostic code, at most 10 -- the point is which classes of error,
    # not the whole log.
    $seen = @{}
    foreach ($line in $errorLines) {
        $code = 'error'
        if ($line -match '(error\s+[A-Za-z]+\d+)') { $code = $Matches[1] }
        if ($seen.ContainsKey($code)) { continue }
        $seen[$code] = $true
        $summary.Add('  ' + $line.Trim())
        if ($seen.Count -ge 10) { break }
    }

    if ($errorLines.Count -eq 0 -and $warnLines.Count -gt 0) {
        $summary.Add('  ' + $warnLines[0].Trim())
    }

    $summary -join "`n" | Write-Output
}
catch {
    # A broken hook must not block development; stay silent.
    exit 0
}

exit 0
