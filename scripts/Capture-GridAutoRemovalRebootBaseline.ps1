[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RepositoryRoot
)

$ErrorActionPreference = 'Stop'
$failurePath = Join-Path $env:ProgramData `
    'GridGuard\validation\M22_REBOOT_BASELINE_ERROR.txt'
trap {
    $directory = Split-Path -Parent $failurePath
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
    [IO.File]::WriteAllText(
        $failurePath,
        ($_ | Out-String),
        [Text.UTF8Encoding]::new($false))
    exit 1
}
. (Join-Path $PSScriptRoot 'GridGuard.LiveValidation.Common.ps1')
$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
if (-not $principal.IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Administrator token is required.'
}
$service = Get-Service -Name GridGuard -ErrorAction Stop
$entry = Get-ItemProperty -LiteralPath `
    'HKLM:\SYSTEM\CurrentControlSet\Services\GridGuard'
if ($service.Status -ne 'Running' -or
    $entry.Start -ne 2 -or
    $entry.DelayedAutoStart -ne 1) {
    throw 'GridGuard must be running with delayed automatic start.'
}
$component = Join-Path ${env:ProgramFiles(x86)} 'NAT Service\natsvc.exe'
$componentKind = if (Test-Path -LiteralPath $component -PathType Leaf) {
    'FILE'
} elseif (Test-Path -LiteralPath $component -PathType Container) {
    'DIRECTORY'
} else {
    'ABSENT'
}
$auditLog = Join-Path $env:ProgramData 'GridGuard\logs\auto-removal.jsonl'
$baseline = [ordered]@{
    ContractId = 'GRIDGUARD-M22-AUTO-REMOVE-NATSERVICE-V1'
    CapturedAt = [DateTimeOffset]::Now.ToString('o')
    BootTime = (Get-CimInstance Win32_OperatingSystem).LastBootUpTime.ToString('o')
    GridGuardState = $service.Status.ToString()
    GridGuardStart = $entry.Start
    GridGuardDelayedAutoStart = $entry.DelayedAutoStart
    GridGuardImagePath = $entry.ImagePath
    NATServicePresent =
        $null -ne (Get-Service -Name NATService -ErrorAction SilentlyContinue)
    NatsvcProcessPresent =
        $null -ne (Get-Process -Name natsvc -ErrorAction SilentlyContinue)
    ExactComponentPathKind = $componentKind
    AuditLineCount = if (Test-Path -LiteralPath $auditLog) {
        @(Get-Content -LiteralPath $auditLog).Count
    } else {
        0
    }
    Protected = Get-LiveValidationProtectedSnapshot
    RepositoryRootHash = (Get-FileHash -LiteralPath (
        Join-Path ([IO.Path]::GetFullPath($RepositoryRoot)) 'GridGuard.sln')
    ).Hash.ToLowerInvariant()
}
$directory = Join-Path $env:ProgramData 'GridGuard\validation'
$path = Join-Path $directory 'M22_REBOOT_BASELINE.json'
New-Item -ItemType Directory -Path $directory -Force | Out-Null
[IO.File]::WriteAllText(
    $path,
    ($baseline | ConvertTo-Json -Depth 10),
    [Text.UTF8Encoding]::new($false))
Write-Output $path
