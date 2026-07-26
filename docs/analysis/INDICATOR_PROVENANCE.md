# Indicator Normalization and Provenance

M04 accepts only defensive indicator records. Every record requires a source type,
source location, extraction method, confidence, and independent-verification flag.
Supported confidence values are `hypothesis`, `observation`, and
`strong-inference`.

The compiler always emits `ruleStatus: candidate`. It cannot emit `confirmed`.
Independent verification is recorded but still requires contextual rule review.
Paths and hashes are normalized deterministically; unsupported types or missing
provenance fail closed.

No real Grid Killer indicators are present because the reference binary was not
available. Synthetic input is used only to validate the pipeline.

