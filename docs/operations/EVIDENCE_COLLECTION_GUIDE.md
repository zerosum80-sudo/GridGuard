# Evidence Collection Guide

The JSON/Markdown evidence package records:

- process inventory and parent process identifiers
- service name, image path, and start configuration
- selected registry persistence values and autoruns
- startup entries and scheduled-task files
- selected filesystem records
- SHA-256, embedded publisher status, product, and version for relevant changed files
- before/after timestamps, typed deltas, timeline, and correlation graph
- rule replay matches
- AuditOnly and Simulate outcomes
- false-positive review findings

Hashing and publisher inspection are demand-driven after a changed record supplies a
relevant path. The publisher status records only embedded-signature presence; it
does not claim certificate-chain trust.

Use disposable output directories. Inspect collection errors. Redact usernames,
hostnames, and private paths before sharing. Do not include the target installer,
reference executable, extracted proprietary content, credentials, or quarantine.
