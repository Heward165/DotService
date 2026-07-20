# Contributing

Changes should preserve DotService's separation between official .NET hosting and operational deployment tooling.

Before submitting a change, run:

```powershell
dotnet format DotService.slnx --verify-no-changes
dotnet build DotService.slnx -c Release
dotnet test tests/DotService.Tests -c Release
dotnet run --project tests/DotService.Conformance -c Release
```

New SCM behavior should include tests for validation and failure paths. Never add credentials to command arguments, fixtures,
logs, or manifests.

