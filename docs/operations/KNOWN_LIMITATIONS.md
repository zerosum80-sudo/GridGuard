# Known Limitations

- The reference binary identity and EA06 payload were statically verified, but
  target-table entries have not been independently verified on a live system.
- AutoIt opcode reconstruction may not reproduce original formatting, comments, or
  source-level intent exactly.
- Candidate rules cover only a conservative subset of distinctive correlated
  process/service pairs; the larger normalized inventory remains candidate evidence.
- Authenticode presence is observed but chain trust is not validated.
- Scheduled-task inventory records task files; action parsing is not yet complete.
- Service IPC identity requires deployment validation under the selected service
  account.
- Real process/service/persistence remediation adapters are disabled.
- Tray actions require the local service and do not enable permanent deletion.
- Packages are unsigned.
