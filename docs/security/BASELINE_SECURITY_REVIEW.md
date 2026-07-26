# Baseline Security Review

Result: pass for the safe release-candidate scope.

- Product and test projects report no known vulnerable NuGet packages.
- Default and sample configuration are AuditOnly.
- Permanent deletion is absent.
- Real process, service, and persistence mutation adapters are disabled.
- Quarantine requires explicit enablement and confirmed score threshold.
- Restore verifies content hash and refuses destination overwrite.
- Service/tray use a local current-user named pipe; no network listener exists.
- Sample/extracted/private/quarantine paths are ignored.
- Static analyzer never launches input.

Residual items are recorded in `KNOWN_LIMITATIONS.md`. This review does not certify
unknown rules or the unavailable reference binary.

