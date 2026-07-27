# Resume Prompt

Resume GridGuard strictly from repository-local state. Read `AGENTS.md`,
`AGENT_STATE.json`, and `AGENT_STATE.must_read_files`.

M22 is approved but blocked: no supported hypervisor is available and no explicit
approved-target identity/path is present. Required inputs are an accessible
disposable Windows VM plus a specifically identified approved target distinct from
the reference binary.

Do not execute the reference binary or install/execute an inferred target. Runtime
remains inactive; AuditOnly and Simulate remain the only permitted validation modes.
