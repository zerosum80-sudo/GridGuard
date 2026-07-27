# M02 Reference Binary Static Identity

Observed: 2026-07-27T15:31:42+09:00

## Identity

- Input: `input/Grid_Killer_v.2.1.4.o_x64.exe`
- Handling: read-only static inspection; execution was not attempted
- Size: 1,125,032 bytes
- SHA-256:
  `8cab9bdcfebb2a5eb6340c6a9f2fdf27737e42af56eddc15322d28d94473217b`
- SHA-1: `d71feb84d57fcb5dde591c60c5069925c728edfb`
- MD5: `a0152f73d3db4e6618c60edae17474f5`
- Expected SHA-256 match: yes

## PE headers

- Format: PE32+ executable
- Machine: AMD64 (`0x8664`)
- Image base: `0x140000000`
- Entry point RVA: `0x1D47C`
- Subsystem: Windows GUI (`2`)
- COFF characteristics: `0x23`
- DLL characteristics: `0x8100`
- Header checksum: `0xC3F26`
- Exported symbols: none
- Authenticode: not signed; security directory size is zero

The COFF timestamp decodes to `2010-04-16T07:47:52Z`. PE timestamps are
operator-controlled metadata and are not reliable evidence of build or release time.

## Sections

| Name | RVA | Virtual size | Raw offset | Raw size | Entropy |
|---|---:|---:|---:|---:|---:|
| `.text` | 4,096 | 613,057 | 1,024 | 613,376 | 6.4280 |
| `.rdata` | 618,496 | 87,708 | 614,400 | 88,064 | 5.1104 |
| `.data` | 708,608 | 117,512 | 702,464 | 30,208 | 2.1706 |
| `.pdata` | 827,392 | 27,156 | 732,672 | 27,648 | 5.7724 |
| `.rsrc` | 856,064 | 111,828 | 760,320 | 112,128 | 5.2527 |

No UPX-named section or section entropy at or above 7.2 was observed. This does not
exclude another compiler wrapper or packing technique.

## Imports

Sixteen libraries are imported:

`WSOCK32.dll`, `VERSION.dll`, `WINMM.dll`, `COMCTL32.dll`, `MPR.dll`,
`WININET.dll`, `PSAPI.DLL`, `USERENV.dll`, `KERNEL32.dll`, `USER32.dll`,
`GDI32.dll`, `COMDLG32.dll`, `ADVAPI32.dll`, `SHELL32.dll`, `ole32.dll`, and
`OLEAUT32.dll`.

The import table includes process, registry, service-control, file, network, GUI,
and COM APIs. These are compatible with the AutoIt runtime and bundled libraries.
They are not attributed to Grid Killer-specific behavior without recovered-script
context.

## Resources and metadata

Twenty-three standard PE resources were inventoried:

- icons: 8
- menu: 1
- dialog: 1
- string tables: 7
- icon groups: 4
- version resource: 1
- manifest: 1

Version strings report:

- `FileVersion`: `3.3.6.1`
- `FileDescription`: `Grid Killer`
- `Comments`: `http://hackerm.blog.me`
- `LegalCopyright`: `Mayday Computer`

The manifest requests `asInvoker` with `uiAccess=false` and Common Controls 6.0.
Raw resources and full resource hashes remain only below ignored
`artifacts/private-analysis/`.

## Overlay and AutoIt assessment

- Overlay offset: 872,448
- Overlay size: 252,584 bytes
- Overlay SHA-256:
  `30757baedc20eb5556b92b4beba2aac57cbec7e6b1b9f644d8ad30d4f20547ea`
- AutoIt location magic offset: 872,448
- `AU3!EA06` marker offset: 872,464
- `AU3!EA05`: not observed
- `AU3!JB01`: not observed

The location magic followed by `AU3!EA06` is strong evidence of an EA06-format
AutoIt payload in the overlay. It is format evidence, not a behavioral indicator.

## Strings

Static extraction produced 4,114 ASCII strings and 963 UTF-16LE strings of at least
five printable characters. Full string inventories remain private and ignored.
Candidate URLs, paths, filenames, registry paths, service identifiers, and command
semantics are promoted only when the recovered-script context supports them.

## Result

M02 binary-dependent residual status: **PASS**. The supplied binary matches the
expected identity and was inspected without execution. No raw binary or proprietary
content is tracked.
