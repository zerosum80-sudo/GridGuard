# M18 Confirmation Policy and False-Positive Controls

Production confirmation is fail-closed. A rule with `confidence: confirmed` must
carry structured evidence under policy `independent-primary-v1`:

- at least two sources with different accountable `controlId` values
- HTTPS provenance and a candidate-specific identity statement
- primary, reproducible evidence outside the reference/removal-table lineage
- no unresolved plausible generic interpretation

The runtime evaluator deduplicates mirrors under the same controlling publisher,
rejects circular and non-primary sources, requires reproducible identity, and only
returns `RecommendConfirmationReview`. It never automatically returns `Confirmed`.
The rule validator independently prevents confirmed rules without two structured,
independently controlled sources.

All six repository rules remain candidate-only. No production response capability
was enabled.
