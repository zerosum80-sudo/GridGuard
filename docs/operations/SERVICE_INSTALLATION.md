# Service Installation

Publish and inspect the service, then use `Install-GridGuardService.ps1 -WhatIf`
before running it in an elevated PowerShell session. The default is `AuditOnly`.
The worker performs initial and periodic reconciliation and exposes status only via
the `GridGuard.Status.v1` current-user named pipe. It has no network listener.

The recovery script configures two delayed restarts. Logs are structured host logs.
Changing mode requires `ExplicitlyEnabled=true` and response validation. Permanent
deletion remains unavailable to the general response framework. The uninstall
script also supports `-WhatIf`.

The installer creates and starts `GridGuard` as a delayed automatic Windows Service
and configures restart recovery. Its exact NATService workflow monitors service
creation, service state changes, and `natsvc.exe` process creation, with periodic
reconciliation as a fallback.

The packaged Worker Service carries a private Windows x64 .NET runtime and does not
depend on a machine-wide runtime installation.

`AutoRemoval.Enabled=true` authorizes only `grid.natservice.001`, exact service name
`NATService`, and exact file
`%ProgramFiles(x86)%\NAT Service\natsvc.exe`. Configuration validation rejects a
different rule, service, or path. Each attempt appends JSONL audit data to
`%ProgramData%\GridGuard\logs\auto-removal.jsonl`.
