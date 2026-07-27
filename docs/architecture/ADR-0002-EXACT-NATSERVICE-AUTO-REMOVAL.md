# ADR-0002: Exact NATService Automatic Removal

Status: accepted.

## Decision

The operator authorizes one narrow production exception to the AuditOnly baseline.
When `AutoRemoval.Enabled=true`, the Windows Service may automatically act only
after `grid.natservice.001` matches an exact `NATService` service name and exact
`%ProgramFiles(x86)%\NAT Service\natsvc.exe` image path belonging to the same
service object.

The fixed action order is:

1. stop the exact NATService component and delete only the exact natsvc file;
2. delete only the NATService registration;
3. verify service, process, file, and rule absence.

Service creation, service state, and process creation are monitored. Periodic
reconciliation remains active. The Windows Service uses delayed automatic start
and configured restart recovery.

## Boundaries

This exception grants no generic candidate-rule mutation authority and does not
alter the existing response executor. The candidate rule keeps all response flags
false. Filebogo, the P2P application, user files, downloads, other services,
directories, and every other rule remain outside the workflow.

Configuration that changes the exact rule ID, service name, or Program Files (x86)
path is rejected. Tests use fakes and perform no host mutation.
