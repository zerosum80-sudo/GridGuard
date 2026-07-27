# Implementation Manifest

| Milestone | Scope | State |
|---|---|---|
| M01 | Repository and AI-OS-compatible governance foundation | Complete |
| M02 | Reference binary static identity | Complete; expected identity and full static inventory verified |
| M03 | AutoIt extraction research and isolated tooling | Complete; EA06 reconstruction and 2-resource extraction succeeded |
| M04 | Indicator normalization | Complete; 115 target rows normalized and 5 conservative candidate rules added |
| M05 | Rule schema and validation | Complete |
| M06 | Core evidence and scoring engine | Complete |
| M07 | Safe system inventory adapters | Complete |
| M08 | Snapshot and diff utility | Complete |
| M09 | Initial scanner and CLI | Complete |
| M10 | Event monitoring | Complete |
| M11 | Safe response framework | Complete |
| M12 | Windows service | Complete |
| M13 | Tray management | Complete |
| M14 | End-to-end synthetic validation | Complete |
| M15 | CI, packaging, baseline release candidate | Complete |
| M16 | Candidate Rule Audit Validation | Complete; 229 normalized candidates, zero current-system matches, 6 refined candidate rules |

## Required top-level deliverables

Canonical governance files, solution/projects, versioned rules, safe monitoring and
response infrastructure, synthetic test coverage, operator/security documentation,
CI, unsigned packaging, and validation evidence.

## Binary-dependent residual completion

The original safe baseline remains intact. M02/M03 were originally partial because
the external sample was absent. On 2026-07-27 the expected sample became available
at the actual repository path, its SHA-256 matched, and the residual static analysis,
EA06 extraction, indicator normalization, candidate-rule validation, and full safe
regression validation passed. Raw binary and extraction output remain ignored.

## M16 completion

M16 added deterministic candidate normalization, targeted read-only current-system
matching, demand-driven matched-file metadata collection, privacy redaction,
promotion-policy enforcement, matched-evidence-only detection output, and safe
AuditOnly/Simulate behavior. Nine duplicate values and two empty rows were removed.
No candidate object matched the current system and no rule was promoted.

Recommended next milestone: M17 Rule confirmation from independent public evidence.
It is not active without explicit operator approval.
