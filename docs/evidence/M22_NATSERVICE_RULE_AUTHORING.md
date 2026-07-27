# M22 NATService Candidate Rule Authoring

- Contract: `GRIDGUARD-M22-PHYSICAL-AUDITONLY-VALIDATION-V1`
- Validation run: `11a4aae1-78b2-4c2a-a786-03aafcebb5e7`
- Evidence ID: `645f012a-9d87-4385-bf3a-37bd51f1ac53`
- Result: `PASS_TRUE_POSITIVE_CANDIDATE_ONLY`

## Rule

`grid.natservice.001` is candidate-only with score 60. It requires both:

- `serviceName equalsIgnoreCase NATService`
- `serviceImagePath endsWithIgnoreCase \NAT Service\natsvc.exe`

The `endsWithIgnoreCase` operator was added to the version 1.0 JSON schema,
semantic validator, and detection engine. A near-match ending in
`natsvc.exe.backup` is rejected by focused testing.

Optional sanitized evidence records the valid NeoNTech publisher, version 3.5.4.90,
SHA-256, `services.exe` parent, and automatic service persistence. These dimensions
are not required predicates and do not satisfy confirmation policy.

Every response flag is false. No confirmation object or confirmed rule was created.
The independent-primary confirmation policy is unchanged.

## Live validation

Two AuditOnly scans reproducibly matched `grid.natservice.001` against only the
`NATService` service-name and image-path evidence. The decision remained Suspicious,
confidence remained strong-inference, and score remained 60.

Simulate produced an observation-only response plan with `Performed=false` and
explicitly proposed no quarantine or host modification.

## Validation

- Release build: 17 projects, 0 errors, 2 NU1900 advisory-feed warnings
- Rule compiler self-test: passed
- Rule validation: 7 rules passed
- Full tests: 54 passed, 0 failed
- Canonical JSON and cross-file consistency: passed
- AuditOnly and non-mutating Simulate: passed
