# Resume Prompt

Resume GridGuard strictly from repository-local state. Read `AGENTS.md`,
`AGENT_STATE.json`, and `AGENT_STATE.must_read_files`.

M22 is `TRUE_POSITIVE_CANDIDATE_ONLY`. The active exact contract is
`GRIDGUARD-M22-PHYSICAL-AUDITONLY-VALIDATION-V1`.

`grid.natservice.001` reproducibly matches the exact NATService name plus image-path
suffix with score 60. It remains candidate-only, grants no mutation response, and
does not satisfy confirmation policy. Seven candidate rules and zero confirmed
rules exist.

Resume by collecting the installed GridGuard service runtime and sanitized
detection log for this Rule ID. Preserve AuditOnly, non-mutating Simulate, and the
existing independent-primary confirmation policy.

Do not install or execute any target or reference binary. Process termination,
deletion, quarantine, registry/service/task/startup/network modification, driver
loading, privilege escalation, remediation, and every destructive action remain
prohibited.
