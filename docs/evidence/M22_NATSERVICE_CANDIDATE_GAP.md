# M22 NATService Candidate Gap Analysis

- Contract: `GRIDGUARD-M22-PHYSICAL-AUDITONLY-VALIDATION-V1`
- Validation run: `edb9b3d1-02f6-42b9-8811-5b47442cff09`
- Evidence ID: `7d5cecbb-74a3-4f09-bcdd-9548b930a9ad`
- Mode: `AuditOnly`
- Result: `PASS_ANALYSIS_BLOCKED_RULE_AUTHORING`

## Observed identity

The user-reported `<PROGRAM_FILES_X86>\HNAT Service` directory does not exist.
The live Windows service configuration identifies the actual path as
`<PROGRAM_FILES_X86>\NAT Service\natsvc.exe`.

| Dimension | Observed value |
|---|---|
| Service | `NATService`, running, automatic start, LocalSystem |
| Process | `natsvc.exe`, PID 10720 |
| Parent | `services.exe`, PID 940 |
| Children | none |
| SHA-256 | `24F3EEF66892FA2CA2C4A4D92CC87E658A15B13DD53176EEF4F0D99B0330D1AE` |
| Signature | Valid; NeoNTech |
| File version | 3.5.4.90 |
| Product version | 1.0.0.0 |
| Size | 4,568,336 bytes |
| Service persistence | automatic Windows service, registry Start=2 |
| Startup entries | none |
| Scheduled tasks | none |
| Loaded modules | unavailable |

No private path, binary, proprietary content, or quarantine data is stored.

## Why current rules do not match

The five real candidate rules require exact service/image-path pairs for FileZzim,
Gridmember, sGrid, TGrid, or USADISK. The sixth rule is a synthetic test fixture.
No current predicate references `NATService`, `natsvc.exe`, NeoNTech, the observed
version or hash, service persistence, or the `services.exe` parent.

The current scanner already supplies `serviceName` and `serviceImagePath`, so a
two-field NATService candidate could be evaluated without expanding runtime
collection. Publisher, version, hash, and process correlation are not initial
scanner predicates in the current live scan path.

A read-only AuditOnly scan reproduced:

`No candidate match. AuditOnly made no changes.`

The service remained running with PID 10720 before and after the scan.

## Conservative candidate proposal

Proposed ID: `grid.natservice.001`

- status: `candidate`
- confidence: `strong-inference`
- score: 60
- required predicates:
  - `serviceName equalsIgnoreCase NATService`
  - `serviceImagePath containsIgnoreCase \NAT Service\natsvc.exe`
- every response flag: false
- confirmed-rule creation: prohibited
- candidate promotion: not recommended

The valid publisher and generic network-service description are material
false-positive considerations. The reference detector observation is not
independent primary confirmation.

## Contract boundary

The active exact contract allows read-only inspection, rule evaluation, sanitized
evidence collection, and repeatable AuditOnly validation only. It does not
authorize candidate-rule authoring. No rule file or confirmed rule was created.

Implementation requires an explicit contract amendment authorizing conservative
candidate-rule creation and focused synthetic validation while preserving
AuditOnly and the current confirmation policy.

## Validation

- GridGuard.Cli Release build: passed, 0 warnings, 0 errors
- Existing rule validation: 6 passed
- Rules and Detection tests: 25 passed, 0 failed
- Canonical JSON, consistency, privacy, and diff checks: passed
