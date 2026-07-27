[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$InputPath
)

$ErrorActionPreference = 'Stop'
$expectedSha256 = '8cab9bdcfebb2a5eb6340c6a9f2fdf27737e42af56eddc15322d28d94473217b'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$privateRoot = Join-Path $repositoryRoot 'artifacts\private-analysis'
$outputPath = Join-Path $privateRoot 'autoit-ripper'
$logPath = Join-Path $privateRoot 'autoit-ripper.log'
$stdoutLogPath = Join-Path $privateRoot 'autoit-ripper.stdout.log'
$environmentPath = Join-Path $repositoryRoot '.venv\autoit-ripper'
$lockPath = Join-Path $repositoryRoot 'tools\GridGuard.BinaryAnalysis\requirements-autoit-ripper.lock'

if (-not (Test-Path -LiteralPath $InputPath -PathType Leaf)) {
    throw "BINARY_NOT_FOUND: $InputPath"
}

$actualSha256 = (Get-FileHash -LiteralPath $InputPath -Algorithm SHA256).Hash.ToLowerInvariant()
if ($actualSha256 -ne $expectedSha256) {
    throw "HASH_MISMATCH: expected $expectedSha256 but observed $actualSha256"
}

$python = Get-Command python -ErrorAction Stop
if (-not (Test-Path -LiteralPath $environmentPath)) {
    & $python.Source -m venv $environmentPath
    if ($LASTEXITCODE -ne 0) { throw "Failed to create isolated Python environment." }
}

$venvPython = Join-Path $environmentPath 'Scripts\python.exe'
& $venvPython -m pip install --disable-pip-version-check --require-hashes -r $lockPath
if ($LASTEXITCODE -ne 0) { throw "Pinned dependency installation failed." }

New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
$previousErrorPreference = $ErrorActionPreference
$ErrorActionPreference = 'Continue'
& $venvPython -m autoit_ripper.cli --verbose --ea guess `
    $InputPath $outputPath 1> $stdoutLogPath 2> $logPath
$extractExitCode = $LASTEXITCODE
$ErrorActionPreference = $previousErrorPreference
Get-Content -LiteralPath $logPath
if ($extractExitCode -ne 0) {
    throw "AutoIt-Ripper did not recover a supported payload. See ignored log: $logPath"
}

Write-Output "Private extraction completed under ignored path: $outputPath"
