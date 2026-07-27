# Exact Task Contract

- Contract ID: `GRIDGUARD-VM-BEHAVIORAL-PREPARATION-V1`
- Contract type: Exact Task Contract
- Starting HEAD: `8d487eb9c4e400f4fd02ad6e042291c2a6bf0ef1`
- State mechanism: repository canonical files
- Execution mode: preparation only
- Terminal state: `READY_FOR_VM_BEHAVIORAL_VALIDATION`

## Scope

Prepare reusable, hypervisor-neutral artifacts for disposable Windows VM behavioral
validation. Implement workflow planning, snapshot comparison, evidence collection
and correlation, timelines, process trees, typed delta engines, file hashes,
publisher/version evidence, rule replay, AuditOnly/Simulate verification, evidence
packages, and false-positive review.

Supported hypervisor abstractions:

- Hyper-V
- VMware
- VirtualBox

No hypervisor is required during M21. An unavailable hypervisor produces
`READY_FOR_VM`, not a failure.

## Safety boundary

M21 must not execute or install a reference binary, webhard client, grid component,
unknown executable, or third-party software. Runtime remains inactive. AuditOnly and
Simulate are the only permitted response modes. Real remediation, quarantine,
permanent deletion, and host-system mutation are prohibited.

All tests use synthetic fixtures, fakes, and temporary directories.

## Milestones

| Milestone | Dependency | State rule |
|---|---|---|
| M21 - VM Preparation | M20 | Active; auto-complete after all preparation validation |
| M22 - Behavioral Validation | M21 | `blocked_by_human_approval`; never auto-continue |
| M23 - Rule Confirmation | M22 | Depends on validated M22 evidence |
| M24 - Production Readiness | M23 | Depends on confirmed-rule review |

## Required workflow

1. Clean Snapshot
2. Install target software - future explicit approval
3. Automatic Snapshot
4. Behavior Collection
5. GridGuard AuditOnly
6. GridGuard Simulate
7. Evidence Correlation
8. Candidate Confirmation
9. Rollback

M21 implements and validates every step except target installation/execution,
behavioral observation of a real component, confirmation from that observation, and
rollback execution.

## Validation

- Release build
- full synthetic tests
- rule validation
- workflow validation
- snapshot validation
- evidence validation
- package validation
- inspection proving no destructive functionality is enabled

## Stop condition

Complete M21 and stop before M22 at
`READY_FOR_VM_BEHAVIORAL_VALIDATION`.
