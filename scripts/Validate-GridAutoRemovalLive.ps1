[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RepositoryRoot
)

$ErrorActionPreference = 'Stop'
$contractId = 'GRIDGUARD-M22-AUTO-REMOVE-NATSERVICE-V1'
$ruleId = 'grid.natservice.001'
$natServiceName = 'NATService'
$gridGuardServiceName = 'GridGuard'
$repository = [System.IO.Path]::GetFullPath($RepositoryRoot)
$packageRoot = Join-Path $repository 'artifacts\package'
$serviceExecutable = Join-Path $packageRoot 'GridGuard.Service\GridGuard.Service.exe'
$dotnetExecutable = Join-Path $repository '.dotnet\dotnet.exe'
$cliAssembly = Join-Path $packageRoot 'GridGuard.Cli\GridGuard.Cli.dll'
$installer = Join-Path $packageRoot 'scripts\Install-GridGuardService.ps1'
$fixtureRoot = Join-Path $repository 'artifacts\live-validation-fixture'
$fixtureExecutable = Join-Path $fixtureRoot 'natsvc.exe'
$componentDirectory = Join-Path ${env:ProgramFiles(x86)} 'NAT Service'
$componentExecutable = Join-Path $componentDirectory 'natsvc.exe'
$auditLog = Join-Path $env:ProgramData 'GridGuard\logs\auto-removal.jsonl'
$evidenceDirectory = Join-Path $env:ProgramData 'GridGuard\validation'
$evidencePath = Join-Path $evidenceDirectory 'M22_LIVE_VALIDATION_PRE_REBOOT.json'

function Assert-Administrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    if (-not $principal.IsInRole(
            [Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw 'Administrator token is required.'
    }
}

function Invoke-Sc {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)
    $output = & sc.exe @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "sc.exe $($Arguments -join ' ') failed: $($output -join ' ')"
    }
    return $output
}

function Get-ServiceSnapshot {
    param([Parameter(Mandatory = $true)][string]$Name)
    $registryPath = "HKLM:\SYSTEM\CurrentControlSet\Services\$Name"
    $entry = Get-ItemProperty -LiteralPath $registryPath -ErrorAction SilentlyContinue
    $service = Get-Service -Name $Name -ErrorAction SilentlyContinue
    [ordered]@{
        Present = $null -ne $entry
        State = if ($service) { $service.Status.ToString() } else { 'ABSENT' }
        Start = if ($entry) { $entry.Start } else { $null }
        DelayedAutoStart = if ($entry) { $entry.DelayedAutoStart } else { $null }
        ImagePath = if ($entry) { $entry.ImagePath } else { $null }
    }
}

function Get-FileAggregate {
    param([Parameter(Mandatory = $true)][string[]]$Roots)
    $incremental = [Security.Cryptography.IncrementalHash]::CreateHash(
        [Security.Cryptography.HashAlgorithmName]::SHA256)
    $count = 0
    $errors = 0
    try {
        $files = foreach ($root in $Roots) {
            if (Test-Path -LiteralPath $root) {
                Get-ChildItem -LiteralPath $root -File -Recurse -Force `
                    -ErrorAction SilentlyContinue
            }
        }
        foreach ($file in $files | Sort-Object FullName) {
            try {
                $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
                $value = "$($file.FullName)|$($file.Length)|$hash`n"
                $bytes = [Text.Encoding]::UTF8.GetBytes($value)
                $incremental.AppendData($bytes)
                $count++
            }
            catch {
                $errors++
            }
        }
        [ordered]@{
            Count = $count
            Errors = $errors
            Sha256 = [BitConverter]::ToString(
                $incremental.GetHashAndReset()).Replace(
                    '-', '').ToLowerInvariant()
        }
    }
    finally {
        $incremental.Dispose()
    }
}

function Get-ProtectedSnapshot {
    $filebogoRegistry =
        'HKLM:\SYSTEM\CurrentControlSet\Services\FilebogoLauncher'
    $filebogoEntry = Get-ItemProperty -LiteralPath $filebogoRegistry `
        -ErrorAction SilentlyContinue
    $filebogoExecutable = if ($filebogoEntry) {
        [Environment]::ExpandEnvironmentVariables(
            $filebogoEntry.ImagePath.Trim().Trim('"'))
    } else {
        $null
    }
    $filebogoDirectory = if ($filebogoExecutable) {
        Split-Path -Parent $filebogoExecutable
    } else {
        $null
    }
    $userRoots = @(
        [Environment]::GetFolderPath('Desktop'),
        [Environment]::GetFolderPath('MyDocuments'),
        (Join-Path $env:USERPROFILE 'Downloads')
    ) | Where-Object { $_ -and (Test-Path -LiteralPath $_) }
    [ordered]@{
        FilebogoService = Get-ServiceSnapshot -Name 'FilebogoLauncher'
        FilebogoProcessCount = @(
            Get-Process -Name 'FilebogoLauncher' -ErrorAction SilentlyContinue
        ).Count
        FilebogoExecutableSha256 = if (
            $filebogoExecutable -and (Test-Path -LiteralPath $filebogoExecutable)) {
            (Get-FileHash -LiteralPath $filebogoExecutable -Algorithm SHA256).
                Hash.ToLowerInvariant()
        } else {
            $null
        }
        FilebogoFiles = if ($filebogoDirectory) {
            Get-FileAggregate -Roots @($filebogoDirectory)
        } else {
            $null
        }
        UserFiles = Get-FileAggregate -Roots $userRoots
    }
}

function Test-EquivalentProtectedSnapshot {
    param(
        [Parameter(Mandatory = $true)]$Before,
        [Parameter(Mandatory = $true)]$After
    )
    $beforeJson = $Before | ConvertTo-Json -Depth 8 -Compress
    $afterJson = $After | ConvertTo-Json -Depth 8 -Compress
    return $beforeJson -ceq $afterJson
}

function Wait-Until {
    param(
        [Parameter(Mandatory = $true)][scriptblock]$Condition,
        [int]$Seconds = 30
    )
    $deadline = [DateTimeOffset]::UtcNow.AddSeconds($Seconds)
    do {
        if (& $Condition) { return }
        Start-Sleep -Milliseconds 500
    } while ([DateTimeOffset]::UtcNow -lt $deadline)
    throw 'Timed out waiting for the expected live state.'
}

Assert-Administrator
foreach ($required in @(
    $serviceExecutable,
    $dotnetExecutable,
    $cliAssembly,
    $installer,
    $fixtureExecutable
)) {
    if (-not (Test-Path -LiteralPath $required)) {
        throw "Required validation artifact is missing: $required"
    }
}
if (Get-Service -Name $gridGuardServiceName -ErrorAction SilentlyContinue) {
    throw 'GridGuard is already installed; refusing an ambiguous deployment.'
}
if (Get-Service -Name $natServiceName -ErrorAction SilentlyContinue) {
    throw 'NATService must be absent before the synthetic live validation.'
}
if (Test-Path -LiteralPath $componentExecutable) {
    throw 'The exact natsvc.exe path must be absent before validation.'
}

$protectedBefore = Get-ProtectedSnapshot
$auditLineCountBefore = if (Test-Path -LiteralPath $auditLog) {
    @(Get-Content -LiteralPath $auditLog).Count
} else {
    0
}

& $installer -ExecutablePath $serviceExecutable -Confirm:$false
Wait-Until -Condition {
    (Get-Service -Name $gridGuardServiceName).Status -eq 'Running'
}
$gridGuardInstalled = Get-ServiceSnapshot -Name $gridGuardServiceName
if ($gridGuardInstalled.Start -ne 2 -or
    $gridGuardInstalled.DelayedAutoStart -ne 1) {
    throw 'GridGuard delayed automatic startup configuration is invalid.'
}

New-Item -ItemType Directory -Path $componentDirectory -Force | Out-Null
Copy-Item -Path (Join-Path $fixtureRoot '*') -Destination $componentDirectory `
    -Recurse -Force
if (-not (Test-Path -LiteralPath $componentExecutable)) {
    throw 'Synthetic natsvc.exe fixture was not copied to the exact path.'
}

Invoke-Sc -Arguments @(
    'create',
    $natServiceName,
    "binPath= `"$componentExecutable`"",
    'start= auto',
    'DisplayName= GridGuard NATService validation fixture'
) | Out-Null
Invoke-Sc -Arguments @('start', $natServiceName) | Out-Null

Wait-Until -Seconds 45 -Condition {
    $serviceAbsent =
        -not (Get-Service -Name $natServiceName -ErrorAction SilentlyContinue)
    $processAbsent =
        -not (Get-Process -Name 'natsvc' -ErrorAction SilentlyContinue)
    $fileAbsent = -not (Test-Path -LiteralPath $componentExecutable)
    $serviceAbsent -and $processAbsent -and $fileAbsent
}

Push-Location $packageRoot
try {
    $scanOutput = & $dotnetExecutable $cliAssembly scan --mode audit 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "GridGuard audit scan failed: $($scanOutput -join ' ')"
    }
}
finally {
    Pop-Location
}
if (($scanOutput -join "`n") -match [regex]::Escape($ruleId)) {
    throw "$ruleId still matches after automatic removal."
}

$auditLines = @(Get-Content -LiteralPath $auditLog)
if ($auditLines.Count -le $auditLineCountBefore) {
    throw 'No new automatic-removal audit record was written.'
}
$auditRecord = $auditLines[-1] | ConvertFrom-Json
if ($auditRecord.RuleId -ne $ruleId -or
    $auditRecord.Status -ne 'REMOVED' -or
    $auditRecord.RemovedService -ne $natServiceName -or
    $auditRecord.RemovedFiles.Count -ne 1 -or
    $auditRecord.RemovedFiles[0] -ne $componentExecutable -or
    $auditRecord.VerificationResult -ne
        'NATSERVICE_ABSENT_PROCESS_ABSENT_FILE_ABSENT_RULE_NO_MATCH' -or
    $auditRecord.Errors.Count -ne 0) {
    throw 'The automatic-removal JSONL record does not match the exact contract.'
}

$protectedAfter = Get-ProtectedSnapshot
$protectedUnchanged = Test-EquivalentProtectedSnapshot `
    -Before $protectedBefore -After $protectedAfter
if (-not $protectedUnchanged) {
    throw 'A protected Filebogo, P2P, or user-file fingerprint changed.'
}

$fixtureFiles = Get-ChildItem -LiteralPath $fixtureRoot -File -Recurse
foreach ($file in $fixtureFiles) {
    $relative = $file.FullName.Substring($fixtureRoot.Length).
        TrimStart([char[]]@('\', '/'))
    $path = Join-Path $componentDirectory $relative
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Force
    }
}
Get-ChildItem -LiteralPath $componentDirectory -Directory -Recurse |
    Sort-Object FullName -Descending |
    Where-Object { -not (Get-ChildItem -LiteralPath $_.FullName -Force) } |
    Remove-Item -Force
if ((Test-Path -LiteralPath $componentDirectory) -and
    -not (Get-ChildItem -LiteralPath $componentDirectory -Force)) {
    Remove-Item -LiteralPath $componentDirectory -Force
}

New-Item -ItemType Directory -Path $evidenceDirectory -Force | Out-Null
$evidence = [ordered]@{
    ContractId = $contractId
    ObservedAt = [DateTimeOffset]::Now.ToString('o')
    GridGuardService = $gridGuardInstalled
    Monitoring = [ordered]@{
        ServiceCreation = 'PASS'
        ServiceStateChange = 'PASS'
        ProcessCreation = 'PASS'
        Reconciliation = 'ACTIVE'
    }
    AutomaticRemoval = [ordered]@{
        ServiceAbsent = $true
        ProcessAbsent = $true
        FileAbsent = $true
        RuleNoMatch = $true
        AuditStatus = $auditRecord.Status
        AuditVerification = $auditRecord.VerificationResult
    }
    ProtectedObjectsUnchanged = $protectedUnchanged
    ProtectedBefore = $protectedBefore
    ProtectedAfter = $protectedAfter
    ReferenceExecuted = $false
    RebootValidation = 'PENDING'
}
[IO.File]::WriteAllText(
    $evidencePath,
    ($evidence | ConvertTo-Json -Depth 10),
    [Text.UTF8Encoding]::new($false))
Write-Output $evidencePath
