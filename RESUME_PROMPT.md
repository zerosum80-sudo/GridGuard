# Resume Prompt

Resume GridGuard strictly from repository-local state. Read `AGENTS.md`,
`AGENT_STATE.json`, and `AGENT_STATE.must_read_files`.

`GRIDGUARD-VM-BEHAVIORAL-PREPARATION-V1` is active at M21. Complete only disposable
VM preparation and synthetic validation, then stop at
`READY_FOR_VM_BEHAVIORAL_VALIDATION`.

M22 requires explicit human approval for real target installation and execution.
Runtime remains inactive; only AuditOnly and Simulate are permitted.
