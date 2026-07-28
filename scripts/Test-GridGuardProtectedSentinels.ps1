[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RepositoryRoot
)

$ErrorActionPreference = 'Stop'
Set-ExecutionPolicy -Scope Process Bypass -Force
. (Join-Path $PSScriptRoot 'GridGuard.LiveValidation.Common.ps1')
if ((Get-Service -Name GridGuard -ErrorAction Stop).Status -ne 'Running') {
    throw 'GridGuard must be running.'
}
$roots = @(
    [Environment]::GetFolderPath('Desktop'),
    [Environment]::GetFolderPath('MyDocuments'),
    (Join-Path $env:USERPROFILE 'Downloads')
) | Where-Object { $_ -and (Test-Path -LiteralPath $_) }
if ($roots.Count -ne 3) {
    throw 'Desktop, Documents, and Downloads are required.'
}
$id = [Guid]::NewGuid().ToString('N')
$sentinels = @()
try {
    foreach ($root in $roots) {
        $path = Join-Path $root ".gridguard-m22-sentinel-$id.txt"
        if (Test-Path -LiteralPath $path) {
            throw "Sentinel already exists: $path"
        }
        [IO.File]::WriteAllText(
            $path,
            "GridGuard protected sentinel $id",
            [Text.UTF8Encoding]::new($false))
        $sentinels += [ordered]@{
            Path = $path
            Sha256 = (Get-FileHash -LiteralPath $path -Algorithm SHA256).
                Hash.ToLowerInvariant()
        }
    }
    $filebogoBefore = Get-LiveValidationFilebogoSnapshot
    Start-Sleep -Seconds 15
    foreach ($sentinel in $sentinels) {
        if (-not (Test-Path -LiteralPath $sentinel.Path -PathType Leaf)) {
            throw 'A protected sentinel was removed.'
        }
        $afterHash = (Get-FileHash -LiteralPath $sentinel.Path -Algorithm SHA256).
            Hash.ToLowerInvariant()
        if ($afterHash -ne $sentinel.Sha256) {
            throw 'A protected sentinel was modified.'
        }
    }
    $filebogoAfter = Get-LiveValidationFilebogoSnapshot
    $filebogoFieldsBefore = [ordered]@{
        Service = $filebogoBefore.FilebogoServicePresent
        State = $filebogoBefore.FilebogoServiceState
        Start = $filebogoBefore.FilebogoServiceStart
        Process = $filebogoBefore.FilebogoProcessCount
        Executable = $filebogoBefore.FilebogoExecutableSha256
        Files = $filebogoBefore.FilebogoFiles
    }
    $filebogoFieldsAfter = [ordered]@{
        Service = $filebogoAfter.FilebogoServicePresent
        State = $filebogoAfter.FilebogoServiceState
        Start = $filebogoAfter.FilebogoServiceStart
        Process = $filebogoAfter.FilebogoProcessCount
        Executable = $filebogoAfter.FilebogoExecutableSha256
        Files = $filebogoAfter.FilebogoFiles
    }
    if (-not (Test-LiveValidationSnapshotEqual `
            -Before $filebogoFieldsBefore -After $filebogoFieldsAfter)) {
        throw 'Filebogo/P2P changed during the protected sentinel window.'
    }
    $result = [ordered]@{
        ContractId = 'GRIDGUARD-M22-AUTO-REMOVE-NATSERVICE-V1'
        ObservedAt = [DateTimeOffset]::Now.ToString('o')
        SentinelCount = $sentinels.Count
        SentinelRoots = @('Desktop', 'Documents', 'Downloads')
        SentinelHashesUnchanged = $true
        FilebogoP2PUnchanged = $true
        GridGuardState = 'Running'
    }
    $output = Join-Path ([IO.Path]::GetFullPath($RepositoryRoot)) `
        'artifacts\live-protected-sentinel-result.json'
    [IO.File]::WriteAllText(
        $output,
        ($result | ConvertTo-Json -Depth 8),
        [Text.UTF8Encoding]::new($false))
    Write-Output $output
}
finally {
    foreach ($sentinel in $sentinels) {
        if (Test-Path -LiteralPath $sentinel.Path) {
            Remove-Item -LiteralPath $sentinel.Path -Force
        }
    }
}
