# PostToolUse hook: 摘要 dotnet build/publish 的 error/warning 行。
# repo 內建（相對 $CLAUDE_PROJECT_DIR），clone 到任何機器即可用，不依賴全域 ~/.claude。
$raw = $input | Out-String
try {
    $j = $raw | ConvertFrom-Json
    $cmd = $j.tool_input.command
    # only process dotnet build/publish calls
    if ($cmd -notmatch '^dotnet (build|publish)') { exit 0 }
    $out = $j.tool_response.output
    $lines = ($out -split "`n") | Where-Object { $_ -match '\d+ Error|\d+ Warning|Build succeeded|FAILED' }
    if ($lines) {
        Write-Host "--- Build Summary ---"
        $lines | ForEach-Object { Write-Host $_.Trim() }
    }
} catch {}
