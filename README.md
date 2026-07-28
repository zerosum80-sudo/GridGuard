# GridGuard

GridGuard is defensive Windows software with one production purpose: monitor for
the exact Grid component identified by `grid.natservice.001` and remove only its
`NATService` service and `natsvc.exe` component. It never removes Filebogo, the P2P
application, downloads, user files, or objects outside that exact rule.

The general response framework remains **AuditOnly** by default. The exact
NATService workflow has separate explicit configuration and fail-closed guards.
The reference binary is never executed or redistributed.

## Project status

See `PROJECT_STATUS.json`, `CURRENT_TASK.md`, and `NEXT_SESSION.md`. Static reference
analysis is closed; the reference binary remains ignored and must never be executed
or redistributed. M22 elevated deployment and physical reboot validation passed.
Live automatic removal remains blocked because NATService did not recreate and the
exact natsvc file path is occupied by a pre-existing directory.

## Safety

Read `SECURITY.md`, `IMPLEMENTATION_RULES.md`, and `DESIGN_FREEZE.md` before making
changes.

## Commands

```text
gridguard status
gridguard scan --mode audit
gridguard rules validate
gridguard rules list
gridguard rules explain <rule-id>
gridguard quarantine list
gridguard quarantine restore <item-id>
gridguard snapshot capture --output <file>
gridguard snapshot diff <before> <after>
gridguard diagnostics
GridGuard.SnapshotDiff workflow validate
GridGuard.SnapshotDiff hypervisors inspect
GridGuard.SnapshotDiff evidence <before.json> <after.json> --output <directory>
```

The Windows Worker Service and tray remain local-only. Build unsigned artifacts with
`scripts/Build-Package.ps1`. Validate the VM preparation workflow with
`scripts/Validate-VmPreparation.ps1`.
