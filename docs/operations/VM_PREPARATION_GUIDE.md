# VM Preparation Guide

## Safety state

M21 prepares a disposable Windows VM workflow. It does not authorize target
installation or execution. GridGuard runtime remains inactive, AuditOnly is the
default, and only AuditOnly/Simulate verification is permitted.

## Hypervisor abstraction

The planning API supports Hyper-V, VMware, and VirtualBox through
`IVmHypervisorAdapter`. Adapters emit reviewable snapshot/rollback command plans;
they never invoke a hypervisor. Availability is informational. If no provider is
available, status is `READY_FOR_VM`.

## Guest baseline

Prepare a disposable Windows 10 x64 or Windows 11 x64 guest with:

- no personal accounts, credentials, mounted host folders, or clipboard sharing
- networking disabled by default and isolated if later approved
- a clean snapshot owned by the operator
- GridGuard unsigned artifacts copied from a verified package
- a dedicated evidence output directory outside monitored target directories
- host/guest clocks recorded in UTC

Do not place the reference binary or target installer into the guest during M21.

## Prepared workflow

Run `GridGuard.SnapshotDiff workflow validate` and
`GridGuard.SnapshotDiff hypervisors inspect`. Review the plan and stop before
`Install target software`. M22 requires explicit human approval.
