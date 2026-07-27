# Release Checklist

- [ ] Release build succeeds.
- [ ] Full test suite succeeds using synthetic fixtures and temporary directories.
- [ ] Formatting verification succeeds.
- [ ] Every rule validates against the canonical schema and semantic validator.
- [ ] AuditOnly remains the default and permanent deletion remains unavailable.
- [ ] Dependency vulnerability review is attempted against the configured advisory
  source; an unavailable feed is recorded rather than represented as a clean scan.
- [ ] No reference binary, extracted content, secrets, logs, or quarantine items
  are tracked.
- [ ] `scripts/Build-Package.ps1` recreates the generated package directory so stale
  files cannot survive from a prior build.
- [ ] Package content excludes `input`, `private-analysis`, and `quarantine`.
- [ ] `PACKAGE_MANIFEST.sha256` contains a SHA-256 entry for every packaged file.
- [ ] Package remains unsigned and unpublished; signing and distribution are
  outside this baseline.
