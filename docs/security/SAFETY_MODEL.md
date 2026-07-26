# Safety Model

`AuditOnly` is the default and performs no host mutation. `Simulate` emits a plan.
`Quarantine` requires explicit enablement, a confirmed decision, score threshold,
and a dedicated file flag. `Remediate` interfaces exist but real process, service,
and persistence mutation adapters are disabled. Permanent deletion is unavailable.

Unknown high-network or high-CPU processes are not automatically malicious.
Unsigned status, AppData execution, and generic filenames are insufficient alone.
All automated tests use synthetic fixtures and temporary directories.

