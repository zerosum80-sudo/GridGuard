# M19 AuditOnly, Simulate, Privacy, and Regression Hardening

## Invariants

- Audit reports copy only explicitly allowlisted evidence fields.
- Non-evidence adapter fields do not cross the report boundary.
- Candidate audit does not mutate its input or any process, service, task,
  registry value, file, or quarantine location.
- Privacy redaction is case-insensitive and idempotent.
- Candidate promotion cannot automatically produce a confirmed decision.
- Production rules retain all response flags as false.
- Permanent deletion remains unavailable.

## Validation

The safe regression gate is the full Release solution build and test suite,
format verification, and validation of every repository rule. AuditOnly and
Simulate are covered using synthetic fixtures and temporary directories only.
