[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param([Parameter(Mandatory = $true)][string]$ExecutablePath)

$ErrorActionPreference = 'Stop'
$resolved = (Resolve-Path -LiteralPath $ExecutablePath).Path
if (-not $PSCmdlet.ShouldProcess('GridGuard', "Install service from $resolved")) { return }
& sc.exe create GridGuard binPath= "`"$resolved`"" start= auto
if ($LASTEXITCODE -ne 0) { throw "sc.exe create failed with $LASTEXITCODE" }
& sc.exe failure GridGuard reset= 86400 actions= restart/60000/restart/60000/""
if ($LASTEXITCODE -ne 0) { throw "sc.exe failure failed with $LASTEXITCODE" }

