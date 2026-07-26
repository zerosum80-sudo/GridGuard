# Binary Analysis Methodology

The reference executable is handled as untrusted data and is never launched.

The local analyzer reads bytes directly and records file hashes, PE machine/magic,
section layout and entropy, import/resource directory presence, embedded EA05/EA06/
JB01 marker observations, and Authenticode certificate presence. Certificate
presence is not equivalent to chain trust. Marker presence is a strong format
inference, not proof of product behavior or a confirmed detection target.

Private extraction output belongs under `artifacts/private-analysis/`, which is
ignored. Only normalized defensive indicators with provenance may enter rules.

## Reproduction

```powershell
.\.dotnet\dotnet.exe run --project tools\GridGuard.BinaryAnalysis -- `
  D:\AI Project\GridGuard\input\Grid_Killer_v.2.1.4.o_x64.exe
```

The command opens the file for reading only. It does not execute or load it.

