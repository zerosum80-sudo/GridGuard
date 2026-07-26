[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$DotnetPath = 'dotnet'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$output = Join-Path $root 'artifacts\package'
New-Item -ItemType Directory -Path $output -Force | Out-Null

foreach ($project in @('GridGuard.Cli', 'GridGuard.Service', 'GridGuard.Tray')) {
    $projectPath = Join-Path $root "src\$project\$project.csproj"
    $projectOutput = Join-Path $output $project
    & $DotnetPath publish $projectPath -c $Configuration --no-restore -o $projectOutput
    if ($LASTEXITCODE -ne 0) { throw "Publish failed for $project." }
}

Copy-Item (Join-Path $root 'rules') (Join-Path $output 'rules') -Recurse -Force
Copy-Item (Join-Path $root 'README.md') $output -Force
Copy-Item (Join-Path $root 'SECURITY.md') $output -Force
Write-Output "Unsigned package directory: $output"

