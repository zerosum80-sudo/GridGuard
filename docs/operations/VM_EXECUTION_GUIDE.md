# VM Execution Guide

This guide applies only after explicit M22 approval. It is not an authorization.

1. Verify the disposable guest identity and clean snapshot.
2. Disable host integration and use an isolated network policy approved for M22.
3. Capture `before.json` with `GridGuard.SnapshotDiff capture`.
4. At the human approval boundary, install and execute only the specifically
   approved target inside the guest.
5. Capture `after.json`; retain timestamps and collection errors.
6. Generate the evidence package with `GridGuard.SnapshotDiff evidence`.
7. Replay candidate rules, then run AuditOnly and Simulate verification.
8. Review correlations and false-positive findings.
9. Make no confirmation claim until the independent-primary evidence policy passes.
10. Stop the guest and use the selected adapter's reviewed rollback plan.

Never enable Quarantine or Remediate, never use permanent deletion, and never move
guest artifacts to an unapproved external service.
