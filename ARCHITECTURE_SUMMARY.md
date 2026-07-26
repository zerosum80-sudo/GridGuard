# Architecture Summary

GridGuard is a defensive Windows utility that observes host inventory and evaluates
normalized evidence against provenance-aware rules. Collection and response are
separated so detection can be exercised safely without modifying the host.

## Data flow

1. Inventory adapters collect process, file, service, autorun, scheduled-task, and
   startup evidence.
2. Monitoring merges event notifications with periodic reconciliation.
3. The rule engine normalizes evidence, applies allowlists and exclusions, evaluates
   boolean/threshold expressions, and produces explained decisions.
4. The response planner maps confirmed decisions to the configured safety mode.
5. Response adapters execute only actions allowed by validated configuration.
6. CLI, service, and tray consume the same application services.

## Trust boundaries

- Reference binaries and extracted data are untrusted and private.
- Rule packages are untrusted input until schema and semantic validation succeed.
- OS mutation occurs only through guarded response interfaces.
- Local status IPC does not expose a network listener.

