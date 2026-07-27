# Exact Task Contract

- Contract ID: `GRIDGUARD-SAFE-BATCH-M17-PLUS-V1`
- Contract type: Exact Task Contract
- State mechanism: existing repository canonical files
- Execution mode: sequential, auto-continue
- Default response modes: `AuditOnly`, `Simulate`
- Runtime activation: prohibited

## Workspace verification

`AIOS-VERIFIED-LOCAL-WORKSPACE-V1` passed before activation:

- repository: `C:\Users\Administrator\Documents\GridGuard`
- branch: `main`
- starting HEAD: `b0e5cc29061a28ea7ceb213f38bfb4da95df47ca`
- worktree: clean
- bootstrap source: `AGENTS.md`, `AGENT_STATE.json`, then
  `AGENT_STATE.must_read_files`
- repository-local canonical state: authoritative

## Approved scope

- independent public-evidence validation
- candidate-rule refinement and non-circular confirmation analysis
- confirmed-rule production hardening only when the canonical confirmation policy passes
- non-destructive public research for unresolved candidates
- AuditOnly and Simulate validation
- false-positive reduction
- privacy, provenance, rule-schema, build, test, CI, documentation, and packaging hardening
- autonomous sequential milestone execution and milestone-specific local commits

## Forbidden scope

- executing the reference executable or any real webhard/grid component
- installing a real webhard/grid component
- downloading unknown executables
- uploading the reference binary or extracted proprietary content
- modifying real processes, services, registry, tasks, or files
- real quarantine, remediation, permanent deletion, or destructive testing outside
  disposable synthetic environments
- enabling response modes beyond AuditOnly or Simulate
- inventing confirmation or using circular evidence
- adding a task engine, orchestration framework, or `.aios` subsystem

## Milestones

| Milestone | Scope | Depends on | Required validation | Auto-continue |
|---|---|---|---|---|
| M17 | Independent Public Evidence Validation | M16 | Candidate-specific primary-source research log; confirmation-policy evaluation; candidate/confirmed counts | Yes |
| M18 | Confirmation Policy and Rule Provenance Hardening | M17 | Focused unit tests; rule-schema validation; false-positive review | Yes |
| M19 | AuditOnly, Simulate, Privacy, and Regression Hardening | M18 | Release build; full tests; formatting; mutation-safety and privacy tests | Yes |
| M20 | CI, Documentation, and Packaging Hardening | M19 | CI-equivalent validation; unsigned package inspection; canonical state consistency | Yes |

## Confirmation gate

A production confirmation requires candidate-specific evidence that is primary,
independent of the reference artifact and of other counted sources, reproducible,
identity-specific, and sufficient to exclude a plausible generic interpretation.
At least two independently controlled qualifying sources are required. Mirrors,
search snippets, filename lists, copied removal tables, and the recovered reference
content do not qualify. Lack of confirmation is a valid terminal result.

## Stop conditions

Continue through M20 without an inter-milestone pause. Stop only if progress
requires a virtual machine, reference or real-component execution, unknown binary
download, proprietary upload, real quarantine/remediation/deletion, system mutation,
or another explicit approval boundary.

