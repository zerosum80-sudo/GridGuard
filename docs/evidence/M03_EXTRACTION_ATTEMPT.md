# M03 AutoIt Extraction Evidence

Observed: 2026-07-27T15:36:00+09:00

## Tool and isolation

- Extractor: `nazywam/AutoIt-Ripper` 1.2.0
- Reviewed upstream commit:
  `d60d449dd779d4f604716c839a16521444ed3b11`
- License: MIT; attribution and reviewed license hash remain in
  `docs/analysis/AUTOIT_EXTRACTION_RESEARCH.md`
- Parser dependency: `pefile==2024.8.26`
- Installation: isolated `.venv/autoit-ripper`, exact wheel hashes required
- Input hash gate: matched expected SHA-256 before parser invocation
- Output boundary: ignored `artifacts/private-analysis/autoit-ripper/`
- Reference executable execution: not attempted

## Version assessment

Static inspection found the AutoIt location magic at file offset 872,448 and
`AU3!EA06` at offset 872,464. The PE version resource reports `3.3.6.1`.
AutoIt-Ripper confirmed an EA06 compressed script blob with matching CRC.

Assessment: **EA06 payload; AutoIt runtime file version 3.3.6.1**. The version
resource is strong compiler/runtime evidence but is not independent build
provenance.

## Extraction path

An initial forced `--ea EA06` attempt failed because that parser route expects a
named `SCRIPT` RCDATA resource. This sample instead stores the AutoIt location
record and EA06 payload in the PE overlay. The reviewed `guess` route first locates
the wrapper record, identifies its EA06 payload, verifies the compressed CRC, and
then reconstructs the script opcodes.

The corrected repository wrapper uses `--ea guess` and retains verbose output in an
ignored log.

## Results

Two bundled members were recovered:

| Member | Size | SHA-256 | Disposition |
|---|---:|---|---|
| reconstructed AutoIt script | 530,693 bytes | `0f1fe96a1c7291ee445e3f404a2e08ee071225686334ba198f5555963d455e69` | private, ignored |
| bundled JPEG | 35,158 bytes | `64df0b0f5bf9b80f3b2cc525878c3efa3dd30cc463e7d40988ab5a7cd6b77ae4` | private, ignored |

The reconstructed script contains 11,603 lines. No raw script line, binary member,
or proprietary resource is tracked.

## Contextual findings

The reconstructed product region contains:

- 115 target-table rows
- 112 distinct executable/process names
- 111 distinct service names
- 6 distinct Run-key value names
- reads of current-user and local-machine Run keys
- reads and writes under the service registry hierarchy
- contextual process termination, file deletion, registry deletion, service
  disabling, and directory-blocking operations
- one external version-check download endpoint
- no recovered scheduled-task creation or deletion logic
- no recovered `sc.exe`, `taskkill`, `schtasks`, or command-shell fragment in the
  product region

Generic AutoIt library functions, imported DLLs, and bundled documentation URLs are
classified as runtime material and are not Grid Killer-specific indicators.

## Result

M03 binary-dependent residual status: **PASS**. EA06 opcode reconstruction and
bundled-resource extraction succeeded. Indicator promotion remains candidate-only
because the target entries have not been independently verified on a live system.
