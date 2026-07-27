# Next Session

Resume by reading the files listed in `AGENTS.md`.

Activate `GRIDGUARD-M22-PHYSICAL-AUDITONLY-VALIDATION-V1` and execute M22 read-only
validation on the dedicated physical Windows test machine.

Inspect existing GridGuard detection output, collect only detection-scoped metadata,
repeat the supported read-only scan, evaluate exact rule predicates and allowlist
precedence, verify no mutation, and store only sanitized evidence. AuditOnly is the
only permitted response mode.

Do not install or execute any target or reference binary. Do not terminate
processes, delete or quarantine files, modify registry/services/tasks/startup or
network configuration, load drivers, escalate privileges, remediate, or perform
any destructive action. Stop if any prohibited authority is required.
