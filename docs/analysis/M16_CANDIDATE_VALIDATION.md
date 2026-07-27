# M16 Candidate Rule Audit Validation

Observed: 2026-07-27T16:00:35+09:00

## Scope and safety

M16 reviewed the recovered Grid Killer target table against the current Windows
system through the existing read-only inventory adapter and detection scanner.
AuditOnly remained the default, runtime remained inactive, and Simulate emitted
plans only. The reference executable and every candidate executable remained
unexecuted.

Raw inventory and matched-file metadata are restricted to ignored
`artifacts/private-analysis/`. The committed summary contains no username, hostname,
private user path, unrelated process, unrelated application, token, or secret.

## Normalization

- source rows reviewed: 115
- raw candidate values reviewed: 238
- duplicate values removed: 9
- all-empty malformed rows removed: 2
- normalized distinct candidates: 229
- executable names: 112
- service names or display names: 111
- Run/RunOnce value names: 6

Normalization applies case-insensitive comparison, quote and whitespace removal,
`.exe` normalization, Windows separators, and registry-hive alias expansion.

## Component-role interpretation

The recovered table is a removal-oriented target inventory, but not every entry is
treated as a removal target:

- 6 names explicitly indicate a grid/P2P component.
- 20 names indicate an updater, launcher, manager, or client application.
- 8 executable names are too generic for standalone use.
- 6 Run-value names are persistence evidence only when their command resolves to a
  correlated executable.
- 95 service strings and 94 other executables remain supporting or actionable
  candidates, not confirmed components.

The recovered script distinguishes process checks, service registry entries,
Run-key persistence, updater/client names, grid-named components, and removal
operations. It does not provide vendor ownership or current deployment proof for
every row.

## Current-system correlation

No candidate process name, referenced executable path, service name/display name,
service ImagePath, Run/RunOnce value, or candidate registry value matched the
current system inventory. Consequently:

- matched candidate files: 0
- demand-driven file hashes: 0
- signer/version inspections: 0
- correlation graph edges: 0
- promotion-eligible findings: 0

This is not a full-disk absence claim. File checks are limited to candidate paths
referenced by a matching process, service, or autorun record. Process names remained
available when process-image access was denied. One scheduled-task directory read
was denied, but no scheduled-task candidate exists in the recovered catalog.

## Rule refinement

The five reference-derived rules now require service name plus service ImagePath.
The Gridmember rule additionally permits a two-of-three correlation with its
autorun command. Filename-only evidence cannot trigger these rules. All response
flags remain false, `permanentDelete` remains false, and all rules remain candidate.

## Result

M16 result: **PASS**. Candidate normalization, targeted current-system audit,
privacy redaction, correlation evaluation, false-positive review, rule refinement,
AuditOnly scan, Simulate scan, and safe validation completed without host mutation.
