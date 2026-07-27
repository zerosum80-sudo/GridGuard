# Design Freeze

Status: **ACTIVE**

The following decisions are approved and frozen for the first production baseline:

- .NET 8 / C# solution.
- Separate Core, Rules, Detection, Monitoring, Response, Service, CLI, and Tray
  projects.
- Separate BinaryAnalysis, RuleCompiler, and SnapshotDiff tools.
- Versioned JSON rules with AND, OR, threshold matching, exclusions, scoring, and
  allowlist precedence.
- Event-driven monitoring plus periodic reconciliation.
- Bounded queues, debounce, deduplication, graceful shutdown, and structured logs.
- Windows Worker Service with local-only IPC and no default network listener.
- Four response modes: AuditOnly, Simulate, Quarantine, Remediate.
- AuditOnly is the default; permanent deletion is excluded.
- Static-only handling of the reference executable.

ADR-0002 records one operator-approved exception without changing the general
response architecture: the explicitly configured `grid.natservice.001` workflow
may remove only the exact NATService component and must verify absence. All other
rules and objects retain the frozen AuditOnly and response-safety boundaries.

A change invalidating these boundaries requires explicit operator approval and an
architecture decision record.
