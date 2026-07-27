# GridGuard Agent Instructions

These rules govern all work in this repository.

## Required reading order

1. `AGENTS.md`
2. `IMPLEMENTATION_RULES.md`
3. `DESIGN_FREEZE.md`
4. `AGENT_STATE.json`
5. `PROJECT_STATUS.json`
6. `CURRENT_TASK.md`
7. `IMPLEMENTATION_MANIFEST.md`
8. `NEXT_SESSION.md`

Repository-local state is authoritative. Do not reconstruct project state from chat,
attachments, or files outside this repository.

## Safety invariants

- GridGuard is defensive software.
- Never execute the reference executable.
- Never commit reference binaries, extracted proprietary content, suspicious samples,
  secrets, private paths, or quarantine contents.
- `AuditOnly` is the default response mode.
- Permanent deletion is outside the production baseline.
- Process termination, service mutation, persistence removal, and file quarantine
  require explicit runtime configuration and confirmed-rule thresholds.
- The sole operator-approved exception is the exact, explicitly enabled
  `grid.natservice.001` workflow defined by
  `GRIDGUARD-M22-AUTO-REMOVE-NATSERVICE-V1`. It may stop and delete only
  `NATService` and delete only `%ProgramFiles(x86)%\NAT Service\natsvc.exe`.
- Tests must use synthetic fixtures, fakes, and temporary directories.
- Preserve unrelated working-tree changes.
- Never redesign the frozen architecture without explicit operator approval.

## Milestone protocol

Implement only the current milestone. Tests and inspection evidence must precede a
completion claim. Update all affected canonical files in the same milestone commit.
If an external input is absent, complete every safe unblocked component and record
the exact blocker.
