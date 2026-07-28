[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RepositoryRoot
)

$ErrorActionPreference = 'Stop'
$repository = [IO.Path]::GetFullPath($RepositoryRoot)
$packageRoot = Join-Path $repository 'artifacts\package'
$installer = Join-Path $packageRoot 'scripts\Install-GridGuardService.ps1'
$executable = Join-Path $packageRoot 'GridGuard.Service\GridGuard.Service.exe'
$evidenceDirectory = Join-Path $env:ProgramData 'GridGuard\validation'
$resultPath = Join-Path $evidenceDirectory 'M22_DEPLOYMENT_RESULT.json'
New-Item -ItemType Directory -Path $evidenceDirectory -Force | Out-Null

try {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    if (-not $principal.IsInRole(
            [Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw 'Administrator token is required.'
    }
    if (Get-Service -Name GridGuard -ErrorAction SilentlyContinue) {
        throw 'GridGuard is already installed.'
    }
    & $installer -ExecutablePath $executable -Confirm:$false
    $deadline = [DateTimeOffset]::UtcNow.AddSeconds(30)
    do {
        $service = Get-Service -Name GridGuard -ErrorAction SilentlyContinue
        if ($service -and $service.Status -eq 'Running') { break }
        Start-Sleep -Milliseconds 500
    } while ([DateTimeOffset]::UtcNow -lt $deadline)
    if (-not $service -or $service.Status -ne 'Running') {
        throw 'GridGuard did not reach RUNNING.'
    }
    $entry = Get-ItemProperty -LiteralPath `
        'HKLM:\SYSTEM\CurrentControlSet\Services\GridGuard'
    if ($entry.Start -ne 2 -or $entry.DelayedAutoStart -ne 1) {
        throw 'GridGuard is not configured for delayed automatic start.'
    }
    $serviceDirectory = Split-Path -Parent $executable
    $privateRuntime = Join-Path $serviceDirectory 'runtime\dotnet.exe'
    $serviceAssembly = Join-Path $serviceDirectory 'GridGuard.Service.dll'
    $imagePathMatchesPackage = (
        $entry.ImagePath -match [regex]::Escape($privateRuntime) -and
        $entry.ImagePath -match [regex]::Escape($serviceAssembly)
    )
    $result = [ordered]@{
        Status = 'PASS'
        ObservedAt = [DateTimeOffset]::Now.ToString('o')
        ServiceState = $service.Status.ToString()
        Start = $entry.Start
        DelayedAutoStart = $entry.DelayedAutoStart
        ImagePathMatchesPackage = $imagePathMatchesPackage
    }
    [IO.File]::WriteAllText(
        $resultPath,
        ($result | ConvertTo-Json -Depth 5),
        [Text.UTF8Encoding]::new($false))
    exit 0
}
catch {
    $result = [ordered]@{
        Status = 'FAIL'
        ObservedAt = [DateTimeOffset]::Now.ToString('o')
        Error = $_.Exception.Message
    }
    [IO.File]::WriteAllText(
        $resultPath,
        ($result | ConvertTo-Json -Depth 5),
        [Text.UTF8Encoding]::new($false))
    exit 1
}
