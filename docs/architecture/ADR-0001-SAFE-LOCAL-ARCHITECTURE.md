# ADR-0001: Safe Local Architecture

Status: accepted.

Use .NET 8 components separated into collection, rules, detection, response, service,
CLI, and tray boundaries. Combine events with reconciliation. Use a current-user
named pipe instead of a network listener. Keep response fail-closed and exclude
permanent deletion. This makes detection testable without granting mutation rights
and preserves a rollback path for quarantined files.

