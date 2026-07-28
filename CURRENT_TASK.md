# Current Task

- Exact Task Contract: `GRIDGUARD-M22-AUTO-REMOVE-NATSERVICE-V1`
- Milestone: M22 Behavioral Validation
- Status: PARTIAL_PASS_BLOCKED_LIVE_AUTO_REMOVAL_TRIGGER
- GridGuard service: RUNNING after physical reboot
- Startup: delayed automatic, restart recovery enabled
- Runtime: packaged private Windows x64 .NET runtime
- General response mode: AuditOnly
- Exact automatic-removal rule: `grid.natservice.001`
- NATService after reboot: absent
- natsvc process after reboot: absent
- Exact natsvc path: occupied by a pre-existing directory, not a file
- AuditOnly result: `NO_MATCH`
- Live automatic removal: not observed because NATService did not recreate
- JSONL live removal record: not produced because no exact trigger occurred
- Filebogo/P2P: service, process, executable, and 125-file aggregate unchanged
- User-file sentinels: Desktop, Documents, Downloads hashes unchanged
- Fixture behavior: fail-closed before mutation at exact-path directory collision
- M23/M24: dependency-blocked
