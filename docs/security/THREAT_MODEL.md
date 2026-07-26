# Threat Model

## Assets and trust boundaries

GridGuard protects host integrity, rule provenance, quarantine recoverability, and
operator control. Untrusted boundaries include observed processes/files, rule JSON,
reference binaries, extracted content, paths, metadata, and local IPC clients.

## Attacker capabilities and abuse paths

- A malicious file may exploit a parser: analysis is static, isolated, hash-gated,
  and private.
- A crafted rule may trigger false remediation: schema/semantic validation,
  allowlist precedence, confirmed thresholds, and explicit modes fail closed.
- A path may escape quarantine: only operator-selected confirmed paths enter the
  store; metadata records canonical original paths and hashes.
- An unprivileged client may control the service: IPC is local and current-user
  restricted; no network listener exists.
- Events may be lost or duplicated: event sources are supplemented by periodic
  reconciliation, bounded queues, and deduplication.
- Quarantine content may be altered: restore verifies SHA-256 and refuses overwrite.

Residual risks include incomplete rules, parser defects, named-pipe identity
differences when installed under a service account, and missing Authenticode chain
verification in the baseline analyzer.

