# Security Policy

## Supported baseline

Security fixes target the current development baseline on Windows 10/11 x64.

## Reporting

Report vulnerabilities privately to the repository owner. Do not attach reference
binaries, proprietary extracted content, quarantine material, secrets, or machine
logs containing private paths to public issues.

## Operating safety

- Keep `AuditOnly` enabled unless remediation is intentionally configured.
- Analyze unknown binaries statically; never run the reference executable.
- Use disposable Windows sandboxes for destructive response validation.
- Treat rules, paths, metadata, and extraction output as untrusted input.
- Automatic permanent deletion is not supported in the initial baseline.

