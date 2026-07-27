# M16 False-Positive Review

## Primary risks

1. Many filenames use generic words such as service, manager, updater, or agent.
2. A webhard client or updater may be a legitimate vendor application even when a
   separate grid/P2P component exists.
3. Process name, unsigned status, AppData location, Run-key presence, or a generic
   service name is insufficient by itself.
4. The recovered script is a historical removal list and may contain stale,
   renamed, duplicated, or vendor-shared components.
5. Matching service and process evidence from the same local object is correlated
   evidence, not independent product confirmation.

## Controls

- Reference-derived rules require a service-name and service-ImagePath combination.
- Gridmember uses a two-of-three service, ImagePath, and autorun correlation.
- Generic filenames are not converted to standalone rules.
- Vendor/updater classifications are separated from explicit grid classifications.
- Detection results retain only rule-matched evidence, reducing unrelated inventory
  disclosure.
- Candidate simulation is observation-only and cannot mutate the host.
- Automatic Confirmed promotion is prohibited.

## Remaining limitations

- Current-system matches were zero, so signer, version, publisher, and product-name
  correlation could not be exercised against a real candidate.
- Read-only registry inventory does not provide authoritative live service state;
  state remains unresolved unless a future approved adapter supplies it.
- No unrestricted drive search was performed.
- Public vendor documentation or another independent evidence source is still
  required before rule confirmation.
