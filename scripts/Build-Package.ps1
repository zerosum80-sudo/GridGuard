[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$DotnetPath = 'dotnet'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$output = Join-Path $root 'artifacts\package'
if (Test-Path -LiteralPath $output) {
    $resolvedOutput = [System.IO.Path]::GetFullPath($output)
    $expectedOutput = [System.IO.Path]::GetFullPath(
        (Join-Path $root 'artifacts\package'))
    if ($resolvedOutput -ne $expectedOutput) {
        throw 'Refusing to clean an unexpected package path.'
    }
    Remove-Item -LiteralPath $resolvedOutput -Recurse -Force
}
New-Item -ItemType Directory -Path $output -Force | Out-Null

foreach ($project in @(
    @{ Name = 'GridGuard.Cli'; Path = 'src\GridGuard.Cli\GridGuard.Cli.csproj' },
    @{ Name = 'GridGuard.Service'; Path = 'src\GridGuard.Service\GridGuard.Service.csproj' },
    @{ Name = 'GridGuard.Tray'; Path = 'src\GridGuard.Tray\GridGuard.Tray.csproj' },
    @{ Name = 'GridGuard.SnapshotDiff'; Path = 'tools\GridGuard.SnapshotDiff\GridGuard.SnapshotDiff.csproj' }
)) {
    $projectPath = Join-Path $root $project.Path
    $projectOutput = Join-Path $output $project.Name
    & $DotnetPath publish $projectPath -c $Configuration --no-restore -o $projectOutput
    if ($LASTEXITCODE -ne 0) { throw "Publish failed for $($project.Name)." }
}

Copy-Item (Join-Path $root 'rules') (Join-Path $output 'rules') -Recurse -Force
New-Item -ItemType Directory -Path (Join-Path $output 'docs\operations') -Force |
    Out-Null
New-Item -ItemType Directory -Path (Join-Path $output 'docs\vm') -Force |
    Out-Null
foreach ($guide in @(
    'VM_PREPARATION_GUIDE.md',
    'VM_EXECUTION_GUIDE.md',
    'EVIDENCE_COLLECTION_GUIDE.md',
    'RULE_CONFIRMATION_GUIDE.md'
)) {
    Copy-Item (Join-Path $root "docs\operations\$guide") `
        (Join-Path $output "docs\operations\$guide") -Force
}
Copy-Item (Join-Path $root 'docs\vm\evidence-package.schema.json') `
    (Join-Path $output 'docs\vm\evidence-package.schema.json') -Force
Copy-Item (Join-Path $root 'docs\vm\false-positive-review-template.md') `
    (Join-Path $output 'docs\vm\false-positive-review-template.md') -Force
Copy-Item (Join-Path $root 'README.md') $output -Force
Copy-Item (Join-Path $root 'SECURITY.md') $output -Force

$forbiddenNames = @('input', 'private-analysis', 'quarantine')
$forbidden = Get-ChildItem -LiteralPath $output -Recurse -Force |
    Where-Object { $forbiddenNames -contains $_.Name }
if ($forbidden) {
    throw "Forbidden package content: $($forbidden.FullName -join ', ')"
}

$manifestPath = Join-Path $output 'PACKAGE_MANIFEST.sha256'
$manifestLines = Get-ChildItem -LiteralPath $output -Recurse -File |
    Where-Object { $_.FullName -ne $manifestPath } |
    Sort-Object FullName |
    ForEach-Object {
        $relative = $_.FullName.Substring($output.Length).
            TrimStart([char[]]@('\', '/')).Replace('\', '/')
        $hash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).
            Hash.ToLowerInvariant()
        "$hash  $relative"
    }
[System.IO.File]::WriteAllLines($manifestPath, $manifestLines)
Write-Output "Unsigned package directory: $output"
Write-Output "Package manifest entries: $($manifestLines.Count)"
