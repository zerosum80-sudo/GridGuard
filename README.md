# GridGuard

GridGuard is defensive Windows software for evidence-based monitoring of known
Korean webhard/P2P grid components. It does not classify software solely because it
uses high CPU/network resources, is unsigned, runs from AppData, or has a generic
filename.

The default mode is **AuditOnly**. Initial rules may be incomplete. Real remediation
must be enabled intentionally and remains guarded. The reference binary is never
executed or redistributed.

## Project status

See `PROJECT_STATUS.json`, `CURRENT_TASK.md`, and `NEXT_SESSION.md`. Static reference
analysis is closed; the reference binary remains ignored and must never be executed
or redistributed. Disposable-VM behavioral preparation is available, while real
target installation and execution remain an explicit approval boundary.

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
