[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param([Parameter(Mandatory = $true)][string]$ExecutablePath)

$ErrorActionPreference = 'Stop'
$resolved = (Resolve-Path -LiteralPath $ExecutablePath).Path
$serviceDirectory = Split-Path -Parent $resolved
$privateRuntime = Join-Path $serviceDirectory 'runtime\dotnet.exe'
$serviceAssembly = Join-Path $serviceDirectory 'GridGuard.Service.dll'
if (-not (Test-Path -LiteralPath $privateRuntime -PathType Leaf) -or
    -not (Test-Path -LiteralPath $serviceAssembly -PathType Leaf)) {
    throw 'The packaged private runtime or GridGuard.Service.dll is missing.'
}
$binaryPath = "`"$privateRuntime`" `"$serviceAssembly`""
if (-not $PSCmdlet.ShouldProcess('GridGuard', "Install service from $resolved")) { return }
& sc.exe create GridGuard binPath= $binaryPath start= delayed-auto
if ($LASTEXITCODE -ne 0) { throw "sc.exe create failed with $LASTEXITCODE" }
& sc.exe failure GridGuard reset= 86400 actions= restart/60000/restart/60000/""
if ($LASTEXITCODE -ne 0) { throw "sc.exe failure failed with $LASTEXITCODE" }
& sc.exe failureflag GridGuard 1
if ($LASTEXITCODE -ne 0) { throw "sc.exe failureflag failed with $LASTEXITCODE" }
& sc.exe start GridGuard
if ($LASTEXITCODE -ne 0) { throw "sc.exe start failed with $LASTEXITCODE" }
