# Next Session

Resume by reading the files listed in `AGENTS.md`.

M22 remains active under `GRIDGUARD-M22-AUTO-REMOVE-NATSERVICE-V1`. The exact
automatic-removal workflow is implemented and validated with synthetic fakes. It
acts only on `grid.natservice.001`, `NATService`, and
`%ProgramFiles(x86)%\NAT Service\natsvc.exe`.

The next allowed operation is elevated deployment of the packaged GridGuard
Windows Service followed by live M22 validation. Verify delayed automatic startup,
restart recovery, the three monitoring event classes, ordered exact removal, JSONL
audit content, and service/process/file/rule absence.

Do not touch Filebogo, the P2P application, user files, downloads, or objects
outside the exact rule. Do not execute the reference binary. Do not begin M23 or
M24.
