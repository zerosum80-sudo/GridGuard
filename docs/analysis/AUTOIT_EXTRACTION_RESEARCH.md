# AutoIt Extraction Research

## Selected dependency

- Project: `nazywam/AutoIt-Ripper`
- Release: `1.2.0`
- Git tag commit:
  `d60d449dd779d4f604716c839a16521444ed3b11`
- License: MIT
- Reviewed license SHA-256:
  `444573f0a6dfad5e68a372f97b13b814f10a22d52735c7fbda7865449397a9c6`
- Runtime dependency: `pefile==2024.8.26` (MIT)

The release advertises EA05, EA06, and JB01 extraction. The reviewed CLI reads the
input as bytes, dispatches to parsers, creates the requested output directory, and
writes extracted members using basename-only paths. The reviewed code contained no
subprocess, shell, socket, registry, ctypes, dynamic evaluation, or recursive-delete
calls. Its only declared dependency is `pefile`.

This review does not make malformed-input handling trustworthy. The extractor must
run in an isolated Python environment, receive only the statically verified sample,
and write solely below ignored `artifacts/private-analysis/`.

## Supply-chain controls

The lock file pins wheel hashes for AutoIt-Ripper 1.2.0 and pefile 2024.8.26.
`Invoke-AutoItExtraction.ps1` verifies the operator-supplied sample SHA-256 before
installing or running the parser. A hash mismatch fails closed. Extracted content is
never staged or published.

## Command

```powershell
.\scripts\Invoke-AutoItExtraction.ps1 `
  -InputPath 'C:\Users\Administrator\Documents\GridGuard\input\Grid_Killer_v.2.1.4.o_x64.exe'
```

Do not execute the input file.
