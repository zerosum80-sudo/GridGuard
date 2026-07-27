# M16 Current-System Audit Results

| Evidence type | Matches |
|---|---:|
| candidate processes | 0 |
| candidate executable references | 0 |
| candidate files verified present | 0 |
| candidate Windows services | 0 |
| candidate Run/RunOnce entries | 0 |
| candidate registry values or service keys | 0 |
| matched-file hashes | 0 |
| signer/version metadata records | 0 |
| correlation relationships | 0 |

## Execution

- Candidate catalog audit: PASS; 115 rows and 229 normalized candidates
- AuditOnly scanner: PASS; no candidate match, no action
- Simulate scanner: PASS; no candidate match, no proposed mutation
- Reference executable execution: not attempted
- Candidate executable execution: not attempted
- Endpoint contact: not attempted
- Host mutation: none

The ignored raw report records 159 process-image access failures while retaining
process-name coverage, plus one inaccessible scheduled-task directory. Neither
condition concealed a name-level candidate match. No scheduled-task candidate was
present.

## Evidence boundary

No full inventory is committed. The machine-readable committed summary contains
counts, classifications, validation results, and an empty correlation graph only.
