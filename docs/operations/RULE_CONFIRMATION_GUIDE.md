# Rule Confirmation Guide

Behavioral evidence strengthens a candidate but does not independently confirm it.

For each candidate:

1. Correlate process, service, registry, autorun, task, and file deltas.
2. Verify SHA-256, product/version metadata, and publisher evidence.
3. Review the process tree and timeline for causality.
4. Replay the candidate rule against the captured snapshot.
5. Document generic interpretations and allowlist candidates.
6. Require two independently controlled, candidate-specific primary sources under
   `independent-primary-v1`.
7. Treat mirrors, search snippets, the reference removal table, and the same VM
   observation as non-independent.
8. Keep the rule candidate-only when evidence is incomplete or ambiguous.

Promotion is a reviewed M23 action after M22 evidence exists. The replay engine never
promotes rules automatically.
