[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param()

$ErrorActionPreference = 'Stop'
if (-not $PSCmdlet.ShouldProcess('GridGuard', 'Delete service registration')) { return }
& sc.exe stop GridGuard
& sc.exe delete GridGuard
if ($LASTEXITCODE -ne 0) { throw "sc.exe delete failed with $LASTEXITCODE" }

