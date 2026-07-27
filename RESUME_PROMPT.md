# Resume Prompt

Resume GridGuard strictly from repository-local state. Read `AGENTS.md`,
`AGENT_STATE.json`, and `AGENT_STATE.must_read_files`.

M22 is `READY_FOR_PHYSICAL_AUDITONLY_VALIDATION`. The active exact contract is
`GRIDGUARD-M22-PHYSICAL-AUDITONLY-VALIDATION-V1`.

Inspect only the existing detection output and detection-scoped live metadata on
the dedicated physical Windows test machine. Use AuditOnly only, repeat supported
read-only validation, verify exact rule predicates and allowlist precedence, and
store only sanitized evidence.

Do not install or execute any target or reference binary. Process termination,
deletion, quarantine, registry/service/task/startup/network modification, driver
loading, privilege escalation, remediation, and every destructive action remain
prohibited.
