# Resume Prompt

Resume GridGuard strictly from repository-local state. Read `AGENTS.md`,
`AGENT_STATE.json`, and `AGENT_STATE.must_read_files`.

M22 is `INSUFFICIENT_EVIDENCE`. The active exact contract is
`GRIDGUARD-M22-PHYSICAL-AUDITONLY-VALIDATION-V1`.

Resume only when an actual GridGuard AuditOnly runtime and its detection output are
observable. The prior read-only observation found no GridGuard process, service,
status pipe, log, Rule ID, or reproducible candidate match. Two scans returned no
candidate match and no changes.

NATService evidence supports a candidate-only service-name plus image-path design,
but rule authoring remains outside the active read-only contract. Require an
explicit M22 contract amendment before creating the rule. Preserve AuditOnly, zero
response flags, and the existing confirmation policy.

Do not install or execute any target or reference binary. Process termination,
deletion, quarantine, registry/service/task/startup/network modification, driver
loading, privilege escalation, remediation, and every destructive action remain
prohibited.
