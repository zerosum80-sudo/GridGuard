# Current Task

- Exact Task Contract: `GRIDGUARD-M22-AUTO-REMOVE-NATSERVICE-V1`
- Milestone: M22 Behavioral Validation
- Status: IMPLEMENTED_AWAITING_ELEVATED_DEPLOYMENT_VALIDATION
- General response mode: AuditOnly
- Exact automatic-removal rule: `grid.natservice.001`
- Exact service: `NATService`
- Exact component: `%ProgramFiles(x86)%\NAT Service\natsvc.exe`
- Monitoring: service creation, service state changes, process creation, periodic
  reconciliation
- Action: stop exact component, delete exact file, delete exact service
- Verification: NATService absent, natsvc process/file absent, rule `NO_MATCH`
- Audit: JSONL detection time, rule, removed service/files, verification, errors
- Outside scope: Filebogo, P2P application, user files, downloads, all other rules
- Service deployment: delayed automatic start and restart recovery configured
- Live installation: not performed by synthetic implementation validation
- M23/M24: dependency-blocked
