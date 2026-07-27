# Operator Guide

Start with `gridguard status`, validate rules, then run `gridguard scan --mode audit`.
Treat suspicious results as observations requiring review. Confirmed results remain
non-mutating unless an explicit response configuration is deployed.

For false positives, capture the full explanation and affected objects, verify signer
and hash independently, then author the narrowest possible allowlist rule. Never
allowlist only by a generic filename or broad directory.

Quarantine records include original path, SHA-256, rule ID, time, and restore ID.
Restore refuses changed content and existing destination files. Back up important
data before enabling quarantine. Initial rules may be incomplete.

The separately authorized automatic workflow is limited to
`grid.natservice.001`. It requires the exact NATService name and exact natsvc path
on the same service object, stops that component, deletes only that file, deletes
only that service registration, and verifies:

- `NATService` absent
- `natsvc.exe` process and file absent
- `grid.natservice.001` returns `NO_MATCH`

Filebogo, the P2P application, user files, downloads, and every other rule remain
outside its authority.
