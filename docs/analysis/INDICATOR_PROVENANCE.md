# Indicator Normalization and Provenance

## Evidence source

The supplied binary matched the approved SHA-256. AutoIt-Ripper 1.2.0 recovered an
EA06 opcode reconstruction after validating the embedded compressed-data CRC. Raw
source and bundled resources remain below ignored `artifacts/private-analysis/`.

The private normalized inventory records, for every target-table row:

- type and value
- recovered-script table context
- extraction method
- `StrongCandidate` confidence
- independent-verification state
- generic-runtime state
- candidate-rule suitability

No row is independently verified. No string occurrence is classified as Confirmed.

## Candidate inventory

| Type | Distinct count | Confidence | Rule suitability |
|---|---:|---|---|
| executable/process name | 112 | StrongCandidate | only with correlated service or persistence evidence |
| service name/display string | 111 | StrongCandidate | only with correlated executable evidence |
| Run-key value name | 6 | StrongCandidate | only with matching command/path evidence |
| registry location | 4 | StrongCandidate context | never sufficient by itself |
| external version endpoint | 1 | StrongCandidate context | monitoring/provenance only |
| promotional/operator URL | 6 | WeakCandidate | unsuitable for detection |
| imported runtime DLL | 16 | RuntimeGeneric | unsuitable by itself |
| scheduled-task identifier | 0 | Unresolved | none recovered |

The target table contains 115 rows. Duplicate rows and duplicate service names
explain why row count exceeds distinct-value counts.

## Behavior context

The product-specific recovered region uses target-table values in:

- process existence checks and one termination path
- service `ImagePath` and `Start` reads
- current-user and local-machine Run-key reads and deletion paths
- service disabling through `Start=4`
- file deletion followed in some branches by directory creation at the same path
- a service-key deletion path
- a version check against
  `http://tistory1.daumcdn.net/tistory/164736/skin/images/Grid_Killer.txt`

These observations support the table's defensive relevance. They do not establish
that every listed component is unwanted, currently deployed, or safe to remove.

## Rule-promotion policy

Candidate rules require correlated values from the recovered table and remain
disabled from confirmed response. Generic names, broad registry locations, imported
APIs, unsigned status, and URLs are excluded as standalone matches. Confirmed rules
require independent system or vendor evidence not present in this analysis.
