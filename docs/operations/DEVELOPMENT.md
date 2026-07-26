# Development Guide

Use .NET 8. Restore with `NuGet.Config`, build `GridGuard.sln`, run the full test
suite, verify formatting, and run `gridguard rules validate`. Tests must use fakes,
synthetic processes, and temporary directories. Do not place samples in Git.

The unsigned package script publishes CLI, Service, and Tray plus rules and safety
documentation. Code signing and public release are intentionally out of scope.

