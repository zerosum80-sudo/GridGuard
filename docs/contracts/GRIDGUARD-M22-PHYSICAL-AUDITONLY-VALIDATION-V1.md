# Exact Task Contract

- Contract ID: `GRIDGUARD-M22-PHYSICAL-AUDITONLY-VALIDATION-V1`
- Contract type: Exact Task Contract
- Starting HEAD: `8904b339825d7f0e07b6a7de08c5e29f72c023b7`
- State mechanism: existing repository canonical files
- Milestone: M22 Behavioral Validation
- Execution environment: dedicated physical Windows test machine
- Response modes: `AuditOnly`; non-mutating `Simulate` for candidate validation
- Runtime mutation: prohibited

## Amendment boundary

This contract supersedes the disposable-VM execution-environment restriction of
`GRIDGUARD-VM-BEHAVIORAL-PREPARATION-V1` for M22 only. It does not alter M21
history, the frozen architecture, confirmation policy, or any safety restriction
other than permitting read-only inspection on the physical test machine.

The contract authorizes observation of software already running on the test
machine. It does not authorize installation, launch, or execution of a target,
reference binary, unknown executable, or third-party component.

## Allowed scope

Only the following operations are allowed:

- read-only live inspection
- `AuditOnly` detection verification
- rule evaluation with allowlist precedence
- candidate-rule authoring from sanitized M22 evidence
- focused synthetic validation of candidate rules
- sanitized evidence collection
- repeatable read-only validation

Candidate-rule authoring authority is limited to status `candidate`, response score
and predicates supported by sanitized M22 evidence, and response flags that grant no
host mutation. Confirmed-rule creation and confirmation-policy changes remain
prohibited.

## Authorized evidence

- running processes and process tree
- executable path
- demand-driven SHA-256
- publisher and version information
- loaded modules
- services
- scheduled tasks
- startup entries
- filesystem metadata
- detection logs
- rule matches and explanations
- evidence JSON
- `AuditOnly` output

Evidence must be sanitized before repository storage. Private paths, personal
information, secrets, raw binaries, proprietary contents, quarantine data, and
unrelated system inventory must not be committed.

## Prohibited scope

- executing the reference binary
- installing, launching, or executing target or unknown software
- process termination
- file deletion
- quarantine
- registry modification
- service modification
- persistence removal
- scheduled-task or startup-entry modification
- network configuration changes
- driver loading
- privilege escalation
- response modes other than `AuditOnly` or non-mutating `Simulate`
- confirmed-rule creation or candidate promotion
- modification of the independent-primary confirmation policy
- remediation, rollback mutation, or any destructive action
- uploading or committing reference binaries, proprietary content, secrets,
  private paths, or quarantine contents

## Validation procedure

1. Confirm `AuditOnly` and record the pre-validation read-only state.
2. Inspect existing GridGuard detection output without changing runtime state.
3. Identify only objects implicated by the detection result.
4. Collect authorized metadata and demand-driven hashes.
5. Evaluate exact rule predicates, confidence, candidate/confirmed status,
   explanation, provenance, and allowlist precedence.
6. Repeat the supported read-only scan and compare results.
7. Verify that no process, file, registry, service, task, startup, network, or
   driver mutation was requested or performed.
8. Store only sanitized JSON and Markdown evidence.

Candidate promotion remains prohibited unless the existing independent-primary
confirmation policy is fully satisfied. Behavioral evidence from this machine is
one evidence control and is not independently sufficient by itself.

## Candidate-rule authoring amendment

On 2026-07-27, candidate-only rule authoring was approved for
`grid.natservice.001`. The rule must require both `NATService` service-name evidence
and an executable path ending in `\NAT Service\natsvc.exe`, use score 60, retain
candidate status, and set every mutation response flag to false. Optional publisher,
version, SHA-256, parent-process, and automatic-service evidence may be retained as
sanitized provenance but cannot independently confirm the rule.

## Required validation

- canonical JSON and cross-file consistency
- Release build
- relevant safe tests
- rule validation
- inspection proving the contract exposes no mutation authority

## Stop conditions

Stop immediately if validation requires target execution, reference execution,
installation, privilege escalation, mutation, destructive action, access to
unapproved proprietary contents, or a response mode other than `AuditOnly`.

M23 remains dependency-blocked until M22 produces validated, sanitized behavioral
evidence. This contract does not auto-continue into M23 or M24.
