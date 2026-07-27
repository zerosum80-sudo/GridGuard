# M16 Indicator Classification

| Classification | Count | Interpretation |
|---|---:|---|
| ActionableCandidate | 94 | removal-table executable requiring correlation |
| SupportingCandidate | 95 | service identity requiring ImagePath or process support |
| GenericRuntime | 8 | ambiguous filename; excluded as a standalone match |
| VendorApplication | 20 | likely updater, launcher, manager, or client application |
| PotentialGridComponent | 6 | explicit grid/P2P naming, still unconfirmed |
| PersistenceIndicator | 6 | Run/RunOnce value requiring command/path correlation |
| RemovalException | 0 | none explicitly recovered |
| Malformed | 2 removed rows | all candidate fields empty |
| Duplicate | 9 removed values | case-insensitive normalized duplicates |
| Unresolved | 0 normalized values | reserved for incomplete future evidence |

## Removed duplicates

- executable: `ExBCSvc.exe`
- services: `BonService`, `Extended Brower Controler Service`
- Run values: `BonUpdate.exe`, `gridmember.exe`, `KAutoUp.exe`,
  `QAutoUp.exe`, `ShareBox`, `v_member.exe`

## Generic standalone exclusions

The following recovered executable names are too broad for standalone detection:

`BigService.exe`, `ExpressService.exe`, `FileService.exe`, `KService.exe`,
`Respon.exe`, `SvcEnv.exe`, `TaskSvc.exe`, and `VManager.exe`.

Generic AutoIt functions, Windows DLL imports, standard Run-key locations, unsigned
status, and generic installation directories are excluded as standalone evidence.

## Confidence mapping

- Recovered table plus operation context: candidate provenance only.
- Current object without correlation: WeakCandidate.
- Current service/process/autorun plus existing hashed executable:
  StrongCandidate.
- Two independent non-circular external sources with no plausible generic meaning:
  recommendation for confirmation review, never automatic Confirmed.
