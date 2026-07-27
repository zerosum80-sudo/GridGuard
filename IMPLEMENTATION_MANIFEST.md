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
| M17 | Independent Public Evidence Validation | Complete; zero qualifying candidate-specific primary sources, zero promotions |
| M18 | Confirmation Policy and Rule Provenance Hardening | Complete; typed non-circular gate, structured confirmed-rule provenance, 22 focused tests and 6-rule validation pass |
| M19 | AuditOnly, Simulate, Privacy, and Regression Hardening | Complete; evidence allowlist/privacy tests added; Release build, 39 tests, formatting, and 6-rule validation pass |
| M20 | CI, Documentation, and Packaging Hardening | Complete; CI-equivalent checks, 75-entry package manifest, advisory scan, docs, and canonical closure pass |
| M21 | VM Preparation | Complete; hypervisor-neutral workflow, collectors, replay, evidence packaging, 52 tests, and validation gates pass |
| M22 | Behavioral Validation | Blocked; NATService gap analyzed, but candidate-rule authoring is outside the active read-only contract |
| M23 | Rule Confirmation | Registered; depends on M22 validated behavioral evidence |
| M24 | Production Readiness | Registered; depends on M23 confirmed-rule review |

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

M17-M20 are registered by `GRIDGUARD-SAFE-BATCH-M17-PLUS-V1`. The batch uses the
existing canonical state mechanism and permits sequential auto-continuation only
inside its non-destructive AuditOnly/Simulate scope.

## Safe batch completion

M17-M20 are complete. Public evidence did not satisfy confirmation policy, so all
six rules remain candidate-only. The next approval boundary is disposable-VM
acquisition or execution of a real component; it is not authorized.

## VM preparation boundary

M21 is complete under `GRIDGUARD-VM-BEHAVIORAL-PREPARATION-V1`. The repository is
`READY_FOR_VM_BEHAVIORAL_VALIDATION`. M22 approval was subsequently received;
M23 and M24 remain dependency-blocked until M22 evidence exists.

## M22 approval preflight

Human approval for M22 was received. Read-only preflight found no available Hyper-V,
VMware, or VirtualBox provider and no repository-local approved-target identity.
The only non-placeholder input remains governed by the reference-binary execution
prohibition. M22 is active but externally blocked; no installation or execution
occurred.

## M22 physical-machine request

Physical-machine live validation was requested without permission to amend or
replace the exact disposable-VM contract. The execution path therefore fails closed.
The approved target identity and target filesystem locations are also unspecified.
No baseline capture or live operation occurred.

## M22 physical AuditOnly contract

`GRIDGUARD-M22-PHYSICAL-AUDITONLY-VALIDATION-V1` supersedes only the M22
disposable-VM execution-environment restriction. It authorizes detection-scoped,
read-only inspection and sanitized evidence collection on the dedicated physical
Windows test machine in AuditOnly mode. All prior prohibitions remain active,
including reference or target execution, privilege escalation, mutation,
quarantine, remediation, and deletion. M23 and M24 remain dependency-blocked until
validated M22 evidence exists.

## M22 physical AuditOnly validation result

Read-only validation observed the prohibited reference identity already running and
did not execute or modify it. The normal P2P process and service identities did not
match any of the six candidate rules. No GridGuard process, service, status pipe,
detection log, or Rule ID was available. Two AuditOnly scans returned no candidate
match and no changes. M22 is `INSUFFICIENT_EVIDENCE`; M23 and M24 remain blocked.
Sanitized evidence is stored in
`docs/evidence/M22_PHYSICAL_AUDITONLY_VALIDATION.json` and the corresponding
Markdown report.

## M22 NATService candidate gap

Read-only evidence identified `NATService` as an automatic service running
`natsvc.exe` from the corrected `NAT Service` directory. The file has a valid
NeoNTech signature and version 3.5.4.90. None of the six candidate rules contains
NATService predicates, so AuditOnly correctly returned no candidate match.

A conservative candidate design requires both exact service name and image-path
evidence, retains candidate status, assigns no response authority, and does not
satisfy confirmation policy. The active exact contract authorizes rule evaluation
but not rule authoring, so no rule file was created. Sanitized analysis is stored in
`docs/evidence/M22_NATSERVICE_CANDIDATE_GAP.json` and the corresponding Markdown
report.
