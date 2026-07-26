# Known Limitations

- The reference binary was unavailable; no real indicators or static identity were
  verified.
- Initial rules are synthetic/incomplete.
- Authenticode presence is observed but chain trust is not validated.
- Scheduled-task inventory records task files; action parsing is not yet complete.
- Service IPC identity requires deployment validation under the selected service
  account.
- Real process/service/persistence remediation adapters are disabled.
- Tray actions require the local service and do not enable permanent deletion.
- Packages are unsigned.

