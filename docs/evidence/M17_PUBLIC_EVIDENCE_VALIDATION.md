# M17 Independent Public Evidence Validation

## Method

Research was limited to non-destructive public pages and primary sources. No
executable, installer, sample, or extracted endpoint was accessed. Searches tested
each candidate's exact executable/service pair against likely vendor domains and
official security sources. Search-result snippets, copied removal lists, and pages
without accountable primary provenance were excluded from confirmation.

The identity checks follow public platform guidance:

- Microsoft documents the service database as containing the service name,
  executable path, and start configuration:
  <https://learn.microsoft.com/en-us/windows/win32/services/database-of-installed-services>
- Microsoft documents `ImagePath` as the service binary path:
  <https://learn.microsoft.com/en-us/windows-hardware/drivers/install/hklm-system-currentcontrolset-services-registry-tree>
- Microsoft documents Authenticode signature inspection:
  <https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.security/get-authenticodesignature>
- CISA's Software Removal Guide calls for software identity evidence including
  vendor, product, filename, version, and hashes:
  <https://www.cisa.gov/sites/default/files/2024-06/CEG%20Software%20Removal%20Guide1_TLP_CLEAR_.pdf>

## Candidate-specific result

| Rule | Exact identity pair | Qualifying independent primary sources | Decision |
|---|---|---:|---|
| `grid.filezzim.001` | `FileZzimGService.exe` + `_FILEZZIM_GGUARD_` | 0 | Unresolved candidate |
| `grid.gridmember.001` | `Gridmember.exe` + `gridmember` | 0 | Unresolved candidate |
| `grid.sgrid.001` | `sGridClient.exe` + `sGrid Client` | 0 | Unresolved candidate |
| `grid.tgrid.001` | `TGridService.exe` + `TGridService` | 0 | Unresolved candidate |
| `grid.usadisk.001` | `WEBHARD_Agent.exe` + `USADISK_AGENT` | 0 | Unresolved candidate |

Exact-pair searches on likely vendor domains and official Korean/international
security sources produced no candidate-specific primary evidence. Secondary
filename/removal-list pages were encountered but excluded: they are not independent
of the same removal-table ecosystem, do not establish binary identity, and offer no
reproducible hash, signature, version, or accountable vendor publication.

## Promotion evaluation

- required qualifying sources per candidate: 2
- observed qualifying sources per candidate: 0
- production confirmations: 0
- candidate rules retained: 6, including the synthetic fixture
- rules promoted: 0

No candidate satisfies the contract's non-circular confirmation gate. Lack of
confirmation is the valid M17 outcome. Further confirmation requires new accountable
vendor/security evidence or disposable-VM acquisition and execution, which is an
explicit approval boundary.

