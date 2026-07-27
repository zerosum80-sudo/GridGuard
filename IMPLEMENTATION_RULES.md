# Implementation Rules

## Language and platform

- .NET 8 and C#.
- Supported production targets: Windows 10 x64 and Windows 11 x64.
- Platform-dependent behavior must sit behind interfaces and be testable with fakes.

## Security

- No execution of unknown or reference binaries.
- No persistence, injection, credential access, stealth, antivirus bypass, or
  offensive behavior.
- No network listener by default; local status transport must be access-controlled.
- No destructive integration tests on the development machine.
- Hashing is demand-driven after cheaper relevance checks.

## Detection

- A generic filename, unsigned status, AppData location, CPU use, or network use is
  never sufficient by itself for a confirmed decision.
- Rules and evidence retain provenance, confidence, and explanation.
- Allowlist precedence is mandatory.
- Extracted strings remain candidates until contextually and independently verified.

## Response

- Default mode: `AuditOnly`.
- `Simulate` reports an exact plan without mutation.
- `Quarantine` and `Remediate` are fail-closed and require explicit configuration.
- Permanent deletion is not implemented in the first production baseline.
- Quarantine records must support restoration and integrity validation.
- One narrow exception is authorized by
  `GRIDGUARD-M22-AUTO-REMOVE-NATSERVICE-V1`: when explicitly enabled, a same-object
  exact match for `grid.natservice.001` may remove only the `NATService` service and
  `%ProgramFiles(x86)%\NAT Service\natsvc.exe`, then must verify service, process,
  file, and rule absence. It grants no authority over Filebogo, the P2P
  application, downloads, user files, or any other rule.

## Quality

- Build and relevant tests follow every meaningful implementation unit.
- Full safe tests and canonical-state consistency checks precede milestone commits.
- Claims must be backed by repository-local evidence.
