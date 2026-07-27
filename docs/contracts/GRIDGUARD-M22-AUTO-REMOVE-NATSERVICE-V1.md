# Exact Task Contract

- Contract ID: `GRIDGUARD-M22-AUTO-REMOVE-NATSERVICE-V1`
- Contract type: Exact Task Contract
- Starting HEAD: `28231b81206f8700a582817c90db4baa847734e9`
- State mechanism: existing repository canonical files
- Milestone: M22 Behavioral Validation
- Production target: Windows 10/11 x64
- General response mode: `AuditOnly`
- Exact automatic-removal rule: `grid.natservice.001`

## Authority amendment

This contract and ADR-0002 supersede only the mutation prohibition for the exact
NATService object identified below. They preserve the confirmation policy,
candidate status, general response framework, reference-execution prohibition, and
all safety controls outside this exact workflow.

## Exact authorized scope

- run GridGuard as a delayed automatic Windows Service with restart recovery;
- monitor NATService creation and state changes;
- monitor natsvc process creation;
- periodically reconcile exact rule state;
- require the same service object to provide exact service-name and image-path
  evidence;
- stop only `NATService`;
- delete only `%ProgramFiles(x86)%\NAT Service\natsvc.exe`;
- delete only the `NATService` service registration;
- verify NATService service absence, natsvc process and file absence, and
  `grid.natservice.001 == NO_MATCH`;
- log detection time, rule ID, removed service, removed files, verification result,
  and errors.

Automatic removal must be explicitly enabled. Any changed rule ID, service name, or
path fails configuration validation.

## Prohibited scope

- any action against Filebogo or the normal P2P application;
- any action against user files or downloads;
- directory-recursive deletion;
- any action based on another rule;
- generic candidate-rule mutation;
- reference-binary execution;
- destructive integration tests on the development machine;
- M23, M24, or unrelated-rule work.

## Required validation

- Release build;
- full synthetic test suite;
- formatting verification;
- all candidate-rule validation;
- package construction and manifest verification;
- canonical JSON and cross-file consistency;
- `git diff --check`;
- commit, push, and remote HEAD equality.

Live elevated service installation is a separate deployment validation step.
