# DotService

DotService is a deployment and service-management toolkit for running .NET applications as production-ready Windows
services. It focuses on the work that begins after an application can already run under the .NET Generic Host: validating
its deployment definition, registering it safely, configuring recovery, diagnosing the machine, controlling its lifecycle,
and removing it predictably.

DotService is not another service-hosting runtime and it is not a microservices platform. The optional hosting package uses
Microsoft's supported `Microsoft.Extensions.Hosting.WindowsServices` integration instead of replacing it.

## Components

| Component | Responsibility |
| --- | --- |
| `DotService.Hosting` | Thin Generic Host defaults for console development and Windows service production hosting. |
| `DotService.Cli` | The `dotservice` tool: validate, diagnose, install, query, control, and uninstall. |
| `DotService.Packaging` | Versioned manifests, defensive command-line construction, native SCM access, recovery, and diagnostics. |

The repository targets C# 14 and .NET 10. The reusable Hosting and Packaging libraries also target .NET 8.

## Why it exists

.NET already provides Worker Service, dependency injection, logging, configuration, controlled shutdown, Event Log support,
and the Windows service lifetime. Registering and operating the published executable is still commonly left to handwritten
`sc.exe` or PowerShell commands. That boundary is where quoting errors, excessive privileges, missing recovery policies,
ambiguous startup failures, and configuration drift accumulate.

DotService makes that boundary explicit and testable:

- JSON manifests are versioned and validated before machine state changes.
- Executable paths are always quoted, and arguments use Windows' documented escaping behavior.
- Service operations call the native Service Control Manager instead of parsing localized console output.
- Start, stop, pause, and continue operations wait for real state transitions with cancellation and timeouts.
- Installation rolls back a newly created registration if configuration is only partially applied.
- Delayed automatic start, dependencies, passwordless identities, and restart recovery are first-class settings.
- `dotservice doctor` reports missing files, elevation problems, risky LocalSystem use, temporary paths, and absent recovery.
- Password-bearing user accounts are intentionally excluded from the preview API so secrets never become command-line
  arguments, managed strings, log fields, or manifest values. Use a gMSA or virtual account instead.

## Quick start

Use the official Generic Host in the application:

```csharp
using DotService.Hosting;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.AddDotService("Acme.Worker");
builder.Services.AddHostedService<Worker>();
await builder.Build().RunAsync();
```

Describe the deployment in `dotservice.json`:

```json
{
  "schemaVersion": 1,
  "name": "Acme.Worker",
  "displayName": "Acme Queue Worker",
  "description": "Processes queued work for the Acme platform.",
  "executable": "publish/Acme.Worker.exe",
  "startMode": "automaticDelayed",
  "account": "localService",
  "arguments": ["--contentRoot", "C:\\ProgramData\\Acme"],
  "dependencies": ["Tcpip"],
  "recovery": {
    "restartAttempts": 3,
    "restartDelay": "00:01:00",
    "resetPeriod": "1.00:00:00",
    "applyToNonCrashFailures": true
  }
}
```

Install and operate it from an elevated terminal:

```powershell
dotnet tool install --global DotService.Cli --prerelease
dotservice validate .\dotservice.json
dotservice doctor .\dotservice.json
dotservice install .\dotservice.json
dotservice status Acme.Worker
dotservice stop Acme.Worker
dotservice start Acme.Worker
dotservice uninstall Acme.Worker
```

Use `--replace` for an intentional reinstall, `--no-start` to leave a new service stopped, and `--timeout <seconds>` to
override lifecycle waiting.

## Design boundaries

- Management commands require Windows; manifest parsing and conformance tests are cross-platform.
- Service creation and configuration require an elevated process.
- Pause and continue work only when the hosted runtime advertises those controls. The standard Generic Host lifetime does
  not claim pause support by default.
- DotService does not claim to provide distributed discovery, orchestration, scaling, isolation, or load balancing.
- MSI/WiX generation, signed upgrade bundles, rollback, health probes, Event Log queries, and firewall policy are tracked as
  separate roadmap capabilities rather than being hidden behind an oversized hosting abstraction.

## Clean-room development

The behavioral goals were derived from public documentation for the retired generation of console-to-service helpers,
including `PeterKottas/DotNetCore.WindowsService`, and from current Microsoft Windows service documentation. No upstream
implementation source was copied or adapted. DotService has a new API, new architecture, new tests, and an independent Git
history. See [the clean-room record](docs/clean-room.md).

## Build and verify

```powershell
dotnet restore DotService.slnx
dotnet build DotService.slnx -c Release --no-restore
dotnet test tests/DotService.Tests -c Release --no-build
dotnet run --project tests/DotService.Conformance -c Release --no-build
```

CI additionally runs a Windows-only disposable SCM integration test that publishes the example, installs and starts the
real service, verifies its state, stops and restarts it, and removes it in a `finally` block.

DotService is licensed under the [MIT License](LICENSE). Security reports should follow [SECURITY.md](SECURITY.md).
