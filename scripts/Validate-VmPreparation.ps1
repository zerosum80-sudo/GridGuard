[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$DotnetPath = 'dotnet'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$validationRoot = Join-Path $root 'artifacts\vm-preparation-validation'
$expectedRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $root 'artifacts\vm-preparation-validation'))
if (Test-Path -LiteralPath $validationRoot) {
    $actualRoot = [System.IO.Path]::GetFullPath($validationRoot)
    if ($actualRoot -ne $expectedRoot) {
        throw 'Refusing to clean an unexpected validation path.'
    }
    Remove-Item -LiteralPath $actualRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $validationRoot -Force | Out-Null

$project = Join-Path $root 'tools\GridGuard.SnapshotDiff\GridGuard.SnapshotDiff.csproj'
$workflowJson = & $DotnetPath run --project $project -c $Configuration --no-build -- `
    workflow validate
if ($LASTEXITCODE -ne 0) { throw 'Workflow validation failed.' }
$workflow = $workflowJson | ConvertFrom-Json
if ($workflow.status -ne 'PASS') { throw 'Workflow status is not PASS.' }

$hypervisorJson = & $DotnetPath run --project $project -c $Configuration --no-build -- `
    hypervisors inspect
if ($LASTEXITCODE -ne 0) { throw 'Hypervisor abstraction validation failed.' }
$hypervisors = $hypervisorJson | ConvertFrom-Json
if ($hypervisors.supported.Count -ne 3) {
    throw 'Expected Hyper-V, VMware, and VirtualBox support.'
}

$now = [DateTimeOffset]::UtcNow
$before = @{
    capturedAt = $now.AddMinutes(-1).ToString('O')
    records = @()
    errors = @()
}
$after = @{
    capturedAt = $now.ToString('O')
    records = @(
        @{
            kind = 'service'
            id = 'synthetic-service'
            properties = @{
                serviceName = 'SyntheticService'
                serviceImagePath = 'C:\Synthetic\synthetic.exe'
            }
        },
        @{
            kind = 'registry'
            id = 'synthetic-registry'
            properties = @{
                registryPath = 'HKEY_CURRENT_USER\Software\Synthetic'
                valueName = 'Synthetic'
                valueData = 'fixture'
            }
        }
    )
    errors = @()
}
$beforePath = Join-Path $validationRoot 'before.json'
$afterPath = Join-Path $validationRoot 'after.json'
$before | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $beforePath -Encoding utf8
$after | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $afterPath -Encoding utf8

$diffJson = & $DotnetPath run --project $project -c $Configuration --no-build -- `
    diff $beforePath $afterPath
if ($LASTEXITCODE -ne 0) { throw 'Snapshot validation failed.' }
$diff = $diffJson | ConvertFrom-Json
if ($diff.added.Count -ne 2) { throw 'Snapshot diff did not retain synthetic additions.' }

$evidenceRoot = Join-Path $validationRoot 'evidence'
& $DotnetPath run --project $project -c $Configuration --no-build -- `
    evidence $beforePath $afterPath --output $evidenceRoot | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Evidence generation failed.' }
$evidencePath = Join-Path $evidenceRoot 'evidence-package.json'
$markdownPath = Join-Path $evidenceRoot 'evidence-package.md'
$evidence = Get-Content -LiteralPath $evidencePath -Raw | ConvertFrom-Json
if ($evidence.runtimeActive -ne $false) { throw 'Runtime must remain inactive.' }
if ($evidence.safetyMode -ne 'AuditOnly/Simulate') {
    throw 'Evidence package safety mode is invalid.'
}
foreach ($kind in @(
    'process', 'service', 'registry', 'autorun',
    'scheduledTask', 'startupEntry', 'file'
)) {
    if ($null -eq $evidence.deltas.$kind) {
        throw "Missing evidence delta: $kind"
    }
}
if (!(Test-Path -LiteralPath $markdownPath)) {
    throw 'Markdown evidence package is missing.'
}

Write-Output 'Workflow validation: PASS'
Write-Output "Hypervisor state: $($hypervisors.state)"
Write-Output 'Snapshot validation: PASS'
Write-Output 'Evidence validation: PASS'
