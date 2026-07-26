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

