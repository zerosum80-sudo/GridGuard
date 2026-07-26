# Rule Authoring Guide

Rules use schema 1.0 and require provenance sources. Prefer multiple independent
attributes joined by `all` or a justified threshold. Use exclusions narrowly and
test allowlist precedence. Candidate/extracted observations cannot claim confirmed
confidence. Never confirm solely from CPU, network, signature absence, AppData, or a
generic name. Keep `permanentDelete` false.

