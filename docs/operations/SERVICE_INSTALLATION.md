# Service Installation

Publish and inspect the service, then use `Install-GridGuardService.ps1 -WhatIf`
before running it in an elevated PowerShell session. The default is `AuditOnly`.
The worker performs initial and periodic reconciliation and exposes status only via
the `GridGuard.Status.v1` current-user named pipe. It has no network listener.

The recovery script configures two delayed restarts. Logs are structured host logs.
Changing mode requires `ExplicitlyEnabled=true` and response validation. Permanent
deletion is unavailable. The uninstall script also supports `-WhatIf`.

