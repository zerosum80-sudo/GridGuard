# Current Task

- Exact Task Contract: `GRIDGUARD-M22-PHYSICAL-AUDITONLY-VALIDATION-V1`
- Milestone: M22 Behavioral Validation
- Status: TRUE_POSITIVE_CANDIDATE_ONLY
- Execution path: dedicated physical Windows test machine
- Response mode: AuditOnly only
- GridGuard runtime/status pipe: not observed
- GridGuard detection log and Rule ID: not observed
- Read-only repeat scans: 2; both no candidate match and no changes
- Observed Grid Killer identity: prohibited reference SHA-256; already running and
  not executed or modified by validation
- Candidate rules: 7
- Current-system candidate matches: 1
- Confirmed rules: 0
- NATService evidence: running automatic service; corrected image path
  `<PROGRAM_FILES_X86>\NAT Service\natsvc.exe`; valid NeoNTech signature;
  version 3.5.4.90; parent `services.exe`
- NATService rule: `grid.natservice.001`; candidate, strong-inference, score 60
- Required match: exact `NATService` service plus image path ending in
  `\NAT Service\natsvc.exe`
- AuditOnly: reproducible candidate match, no mutation
- Simulate: observation-only, no quarantine or host modification proposed
- Confirmation policy: unchanged; zero confirmed rules
- M23/M24: dependency-blocked
- Remaining blocker: observe the installed GridGuard service runtime and its
  detection log for `grid.natservice.001`
