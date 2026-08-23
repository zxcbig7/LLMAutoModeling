[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$ProjectDir,

    [int]$Round = -1
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$failures = [System.Collections.Generic.List[string]]::new()

function Add-Failure([string]$Message) {
    $script:failures.Add($Message)
}

function Get-Sha256([string]$Path) {
    (Get-FileHash -Algorithm SHA256 -LiteralPath $Path).Hash
}

function Get-JsonProperty([System.Text.Json.JsonElement]$Element, [string]$Name) {
    foreach ($property in $Element.EnumerateObject()) {
        if ($property.Name -ieq $Name) {
            return $property.Value
        }
    }
    return $null
}

function Get-JsonText($Element) {
    if ($null -eq $Element) { return $null }
    switch ($Element.ValueKind) {
        'String' { return $Element.GetString() }
        'Null' { return $null }
        default { return $Element.GetRawText() }
    }
}

function ConvertTo-CanonicalJson([System.Text.Json.JsonElement]$Element) {
    switch ($Element.ValueKind) {
        'Object' {
            $parts = foreach ($property in @($Element.EnumerateObject() | Sort-Object Name)) {
                # Seed is a repeated measurement condition, not part of a candidate strategy.
                if ($property.Name -in @('Seed', 'randomSeed')) { continue }
                $quotedName = ConvertTo-Json -Compress -InputObject $property.Name
                '{0}:{1}' -f $quotedName, (ConvertTo-CanonicalJson $property.Value)
            }
            return '{' + ($parts -join ',') + '}'
        }
        'Array' {
            return '[' + (($Element.EnumerateArray() | ForEach-Object { ConvertTo-CanonicalJson $_ }) -join ',') + ']'
        }
        'String' { return ConvertTo-Json -Compress -InputObject $Element.GetString() }
        default { return $Element.GetRawText() }
    }
}

function Get-ConfigFingerprint([System.Text.Json.JsonElement]$Config) {
    $canonical = ConvertTo-CanonicalJson $Config
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($canonical)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        ([System.BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '')
    }
    finally {
        $sha.Dispose()
    }
}

function Get-TrialSeed([System.Text.Json.JsonElement]$Config) {
    $tunable = Get-JsonProperty $Config 'tunable'
    $seed = Get-JsonProperty $tunable 'Seed'
    if ($null -ne $seed) { return Get-JsonText $seed }

    $solverSpecific = Get-JsonProperty $Config 'solverSpecific'
    foreach ($name in @('Seed', 'randomSeed')) {
        $seed = Get-JsonProperty $solverSpecific $name
        if ($null -ne $seed) { return Get-JsonText $seed }
    }
    return $null
}

function Get-HistoryFacts([string]$History, [int]$RoundNumber) {
    $pattern = '(?s)<!--\s*TUNING-FACTS:R' + $RoundNumber + ':BEGIN\s*-->\s*```json\s*(?<facts>\{.*?\})\s*```\s*<!--\s*TUNING-FACTS:R' + $RoundNumber + ':END\s*-->'
    $match = [regex]::Match($History, $pattern)
    if (-not $match.Success) { return $null }
    try {
        return [System.Text.Json.JsonDocument]::Parse($match.Groups['facts'].Value)
    }
    catch {
        Add-Failure "R$RoundNumber TUNING-FACTS JSON 無法解析：$($_.Exception.Message)"
        return $null
    }
}

$projectPath = (Resolve-Path -LiteralPath $ProjectDir).Path
$projectName = Split-Path -Leaf $projectPath
$archiveDir = Join-Path $projectPath 'Experiments'
$historyPath = Join-Path $projectPath 'TuningHistory.md'
$programPath = Join-Path $projectPath 'Program.cs'

foreach ($required in @($archiveDir, $historyPath, $programPath)) {
    if (-not (Test-Path -LiteralPath $required)) {
        Add-Failure "缺少必要 Phase 3 路徑：$required"
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { [Console]::Error.WriteLine("FAIL: $_") }
    exit 1
}

$history = Get-Content -Raw -Encoding utf8 -LiteralPath $historyPath
$program = Get-Content -Raw -Encoding utf8 -LiteralPath $programPath
$jsonFiles = Get-ChildItem -LiteralPath $archiveDir -File -Filter "$projectName-tuning-r*.json" |
    Sort-Object Name

if ($jsonFiles.Count -eq 0) {
    Add-Failure "Experiment archive 為空：$archiveDir"
}

$candidateFingerprints = @{}

foreach ($jsonFile in $jsonFiles) {
    if ($jsonFile.BaseName -notmatch ('^' + [regex]::Escape($projectName) + '-tuning-r(?<round>\d+)$')) {
        Add-Failure "Experiment JSON 名稱不符合 <Project>-tuning-r<N>.json：$($jsonFile.Name)"
        continue
    }

    $roundNumber = [int]$Matches['round']
    if ($Round -ge 0 -and $roundNumber -ne $Round) { continue }

    $baseName = $jsonFile.BaseName
    $csvPath = Join-Path $archiveDir "$baseName.csv"
    $trajectoryPath = Join-Path $archiveDir "$baseName-trajectory.csv"
    foreach ($artifact in @($csvPath, $jsonFile.FullName, $trajectoryPath)) {
        if (-not (Test-Path -LiteralPath $artifact)) {
            Add-Failure "R$roundNumber archive 缺少 artifact：$artifact"
        }
    }
    if ($failures.Count -gt 0 -and (-not (Test-Path -LiteralPath $csvPath) -or -not (Test-Path -LiteralPath $trajectoryPath))) {
        continue
    }

    if ($program -notmatch ('(?m)^\s*//\s*R' + $roundNumber + '\s+—\s+' + [regex]::Escape($baseName) + '\s*$')) {
        Add-Failure "R$roundNumber Program.cs 缺少 archive marker：// R$roundNumber — $baseName"
    }
    if ($history -notmatch ('(?m)^##\s+R' + $roundNumber + '\b')) {
        Add-Failure "TuningHistory.md 缺少 R$roundNumber 標題"
    }
    if ($history -notmatch [regex]::Escape("Experiments/$baseName.json")) {
        Add-Failure "R$roundNumber History 未參照 archive JSON：Experiments/$baseName.json"
    }

    try {
        $document = [System.Text.Json.JsonDocument]::Parse((Get-Content -Raw -Encoding utf8 -LiteralPath $jsonFile.FullName))
    }
    catch {
        Add-Failure "R$roundNumber archive JSON 無法解析：$($_.Exception.Message)"
        continue
    }

    try {
        $trials = Get-JsonProperty $document.RootElement 'trials'
        if ($null -eq $trials -or $trials.ValueKind -ne [System.Text.Json.JsonValueKind]::Array -or $trials.GetArrayLength() -eq 0) {
            Add-Failure "R$roundNumber archive JSON 沒有非空 trials 陣列"
            continue
        }

        $factsDocument = Get-HistoryFacts $history $roundNumber
        if ($null -eq $factsDocument) {
            Add-Failure "R$roundNumber History 缺少可驗證的 TUNING-FACTS block"
        }
        else {
            try {
                $factsRoot = $factsDocument.RootElement
                $factsExperiment = Get-JsonText (Get-JsonProperty $factsRoot 'experiment')
                if ($factsExperiment -cne $baseName) {
                    Add-Failure "R$roundNumber TUNING-FACTS experiment 不符：'$factsExperiment' ≠ '$baseName'"
                }
                $factsArchive = Get-JsonProperty $factsRoot 'archive'
                $expectedHashes = @{
                    csvSha256        = Get-Sha256 $csvPath
                    jsonSha256       = Get-Sha256 $jsonFile.FullName
                    trajectorySha256 = Get-Sha256 $trajectoryPath
                }
                foreach ($key in $expectedHashes.Keys) {
                    $actualHash = Get-JsonText (Get-JsonProperty $factsArchive $key)
                    if ($actualHash -cne $expectedHashes[$key]) {
                        Add-Failure ('R{0} TUNING-FACTS {1} 與 archive 不一致' -f $roundNumber, $key)
                    }
                }

                $factsTrials = Get-JsonProperty $factsRoot 'trials'
                if ($null -eq $factsTrials -or $factsTrials.ValueKind -ne [System.Text.Json.JsonValueKind]::Array -or $factsTrials.GetArrayLength() -ne $trials.GetArrayLength()) {
                    Add-Failure ('R{0} TUNING-FACTS trial 數量與 archive JSON 不一致' -f $roundNumber)
                }
                else {
                    for ($i = 0; $i -lt $trials.GetArrayLength(); $i++) {
                        $trial = $trials[$i]
                        $fact = $factsTrials[$i]
                        $metrics = Get-JsonProperty $trial 'metrics'
                        $config = Get-JsonProperty $trial 'config'
                        $expected = @{
                            label          = Get-JsonText (Get-JsonProperty $trial 'label')
                            seed           = Get-TrialSeed $config
                            status         = Get-JsonText (Get-JsonProperty $metrics 'status')
                            objectiveValue = Get-JsonText (Get-JsonProperty $metrics 'objectiveValue')
                            bestBound      = Get-JsonText (Get-JsonProperty $metrics 'bestBound')
                            mipGap         = Get-JsonText (Get-JsonProperty $metrics 'mipGap')
                            runTimeMs      = Get-JsonText (Get-JsonProperty $metrics 'runTimeMs')
                            configFingerprint = Get-ConfigFingerprint $config
                        }
                        foreach ($key in $expected.Keys) {
                            $actual = Get-JsonText (Get-JsonProperty $fact $key)
                            if ($actual -cne $expected[$key]) {
                                Add-Failure ('R{0} Trial[{1}] TUNING-FACTS {2} 與 archive JSON 不一致' -f $roundNumber, $i, $key)
                            }
                        }
                    }
                }
            }
            finally {
                $factsDocument.Dispose()
            }
        }

        foreach ($trial in $trials.EnumerateArray()) {
            $label = Get-JsonText (Get-JsonProperty $trial 'label')
            if ($label -notmatch ('\|\s*r' + $roundNumber + '-(?<config>[^|]+)$')) {
                Add-Failure "R$roundNumber Trial label 缺少 r$roundNumber- 前綴：$label"
                continue
            }
            $configLabel = $Matches['config']
            $config = Get-JsonProperty $trial 'config'
            $fingerprint = Get-ConfigFingerprint $config
            $isBaseline = $configLabel -eq 'baseline'
            $isReplica = $configLabel -match '-replica-of-r\d+'
            if (-not $isBaseline -and -not $isReplica) {
                if ($candidateFingerprints.ContainsKey($fingerprint) -and $candidateFingerprints[$fingerprint].Round -ne $roundNumber) {
                    Add-Failure "R$roundNumber candidate '$label' 與 R$($candidateFingerprints[$fingerprint].Round) '$($candidateFingerprints[$fingerprint].Label)' 的有效 config 完全重複"
                }
                else {
                    $candidateFingerprints[$fingerprint] = [pscustomobject]@{ Round = $roundNumber; Label = $label }
                }
            }
            elseif ($isReplica -and $history -notmatch ('(?is)R' + $roundNumber + '.*replication')) {
                Add-Failure "R$roundNumber replica candidate 未在 History 明確記錄 replication 理由：$label"
            }
        }
    }
    finally {
        $document.Dispose()
    }
}

if ($failures.Count -gt 0) {
    [Console]::Error.WriteLine(("Tuning archive verification failed with {0} issue(s):" -f $failures.Count))
    $failures | ForEach-Object { [Console]::Error.WriteLine("- $_") }
    exit 1
}

Write-Host "PASS: Phase 3 archive, Program.cs markers, TuningHistory facts, and cross-round candidate uniqueness are consistent."
