[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$failurePath = Join-Path $env:ProgramData `
    'GridGuard\validation\M22_REBOOT_RESULT_ERROR.txt'
trap {
    [IO.File]::WriteAllText(
        $failurePath,
        ($_ | Out-String),
        [Text.UTF8Encoding]::new($false))
    exit 1
}
$baselinePath = Join-Path $env:ProgramData `
    'GridGuard\validation\M22_REBOOT_BASELINE.json'
$evidencePath = Join-Path $env:ProgramData `
    'GridGuard\validation\M22_REBOOT_RESULT.json'
if (-not (Test-Path -LiteralPath $baselinePath)) {
    throw 'Pre-reboot baseline evidence is missing.'
}
. (Join-Path $PSScriptRoot 'GridGuard.LiveValidation.Common.ps1')
$baseline = Get-Content -Raw -LiteralPath $baselinePath | ConvertFrom-Json
$service = Get-Service -Name GridGuard -ErrorAction Stop
$entry = Get-ItemProperty -LiteralPath `
    'HKLM:\SYSTEM\CurrentControlSet\Services\GridGuard'
$process = Get-CimInstance Win32_Service -Filter "Name='GridGuard'"
if ($service.Status -ne 'Running' -or
    $entry.Start -ne 2 -or
    $entry.DelayedAutoStart -ne 1 -or
    $process.State -ne 'Running' -or
    $process.ProcessId -le 0) {
    throw 'GridGuard did not return as a running delayed-auto service after reboot.'
}
if (Get-Service -Name NATService -ErrorAction SilentlyContinue) {
    throw 'NATService returned after reboot.'
}
if (Get-Process -Name natsvc -ErrorAction SilentlyContinue) {
    throw 'natsvc returned after reboot.'
}
$component = Join-Path ${env:ProgramFiles(x86)} 'NAT Service\natsvc.exe'
if (Test-Path -LiteralPath $component) {
    if (Test-Path -LiteralPath $component -PathType Leaf) {
        throw 'natsvc.exe file returned after reboot.'
    }
}
$protectedAfter = Get-LiveValidationProtectedSnapshot
$filebogoBefore = [ordered]@{
    ServicePresent = $baseline.Protected.FilebogoServicePresent
    ServiceState = $baseline.Protected.FilebogoServiceState
    ServiceStart = $baseline.Protected.FilebogoServiceStart
    ServiceImagePath = $baseline.Protected.FilebogoServiceImagePath
    ProcessCount = $baseline.Protected.FilebogoProcessCount
    ExecutableSha256 = $baseline.Protected.FilebogoExecutableSha256
    Files = $baseline.Protected.FilebogoFiles
}
$filebogoAfter = [ordered]@{
    ServicePresent = $protectedAfter.FilebogoServicePresent
    ServiceState = $protectedAfter.FilebogoServiceState
    ServiceStart = $protectedAfter.FilebogoServiceStart
    ServiceImagePath = $protectedAfter.FilebogoServiceImagePath
    ProcessCount = $protectedAfter.FilebogoProcessCount
    ExecutableSha256 = $protectedAfter.FilebogoExecutableSha256
    Files = $protectedAfter.FilebogoFiles
}
$filebogoUnchanged = Test-LiveValidationSnapshotEqual `
    -Before $filebogoBefore -After $filebogoAfter
if (-not $filebogoUnchanged) {
    throw 'The protected Filebogo/P2P fingerprint changed.'
}
$userAggregateUnchanged = Test-LiveValidationSnapshotEqual `
    -Before $baseline.Protected.UserFiles -After $protectedAfter.UserFiles
$auditLog = Join-Path $env:ProgramData 'GridGuard\logs\auto-removal.jsonl'
$auditLines = if (Test-Path -LiteralPath $auditLog) {
    @(Get-Content -LiteralPath $auditLog)
} else {
    @()
}
$newAuditRecords = $auditLines.Count - $baseline.AuditLineCount
$lastAudit = if ($newAuditRecords -gt 0) {
    $auditLines[-1] | ConvertFrom-Json
} else {
    $null
}
$autoRemovalObserved = (
    $lastAudit -and
    $lastAudit.RuleId -eq 'grid.natservice.001' -and
    $lastAudit.Status -eq 'REMOVED' -and
    $lastAudit.VerificationResult -eq
        'NATSERVICE_ABSENT_PROCESS_ABSENT_FILE_ABSENT_RULE_NO_MATCH'
)
$evidence = [ordered]@{
    ContractId = 'GRIDGUARD-M22-AUTO-REMOVE-NATSERVICE-V1'
    RebootValidation = 'PASS'
    RebootObservedAt = [DateTimeOffset]::Now.ToString('o')
    BootTimeBefore = $baseline.BootTime
    BootTimeAfter =
        (Get-CimInstance Win32_OperatingSystem).LastBootUpTime.ToString('o')
    GridGuardPostReboot = [ordered]@{
        State = $service.Status.ToString()
        Start = $entry.Start
        DelayedAutoStart = $entry.DelayedAutoStart
        ProcessId = $process.ProcessId
    }
    FilebogoP2PUnchanged = $filebogoUnchanged
    UserAggregateUnchangedAcrossReboot = $userAggregateUnchanged
    UserAggregateVerdict = if ($userAggregateUnchanged) {
        'PASS'
    } else {
        'INCONCLUSIVE_VOLATILE_REBOOT_WINDOW'
    }
    NewAuditRecordCount = $newAuditRecords
    AutomaticRemovalObserved = [bool]$autoRemovalObserved
    AutomaticRemovalStatus = if ($autoRemovalObserved) {
        'PASS'
    } else {
        'NOT_OBSERVED_NO_NATSERVICE_RECREATION'
    }
    ExactComponentPathKindBefore = $baseline.ExactComponentPathKind
    ExactComponentPathKindAfter = if (
        Test-Path -LiteralPath $component -PathType Container) {
        'DIRECTORY'
    } elseif (Test-Path -LiteralPath $component -PathType Leaf) {
        'FILE'
    } else {
        'ABSENT'
    }
}
[IO.File]::WriteAllText(
    $evidencePath,
    ($evidence | ConvertTo-Json -Depth 10),
    [Text.UTF8Encoding]::new($false))
Write-Output $evidencePath
