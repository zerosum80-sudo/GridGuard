# Resume Prompt

Resume GridGuard strictly from repository-local state. Read `AGENTS.md`,
`AGENT_STATE.json`, and `AGENT_STATE.must_read_files`.

`GRIDGUARD-VM-BEHAVIORAL-PREPARATION-V1` is complete. Stop state:
`READY_FOR_VM_BEHAVIORAL_VALIDATION`.

M22 is `blocked_by_human_approval`. The exact approval boundary is installation and
execution of a specifically approved real target inside a disposable Windows VM.
Do not execute the reference binary, install or execute a target, enable runtime,
or use Quarantine, Remediate, or permanent deletion without new explicit approval.
