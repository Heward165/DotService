# Clean-room record

DotService is an independent implementation. Research for its initial behavioral scope was deliberately limited to public
documentation, examples in README files, release descriptions, issue titles, and current Microsoft platform documentation.
The implementation files of the reference project were not inspected or copied.

## Behavioral reference

The public documentation for [PeterKottas/DotNetCore.WindowsService](https://github.com/PeterKottas/DotNetCore.WindowsService)
describes these user-visible capabilities:

- run the same executable interactively or under the Windows Service Control Manager;
- install, uninstall, start, stop, pause, and continue a service;
- set its name, display name, description, identity, startup behavior, and dependencies;
- configure lifecycle callbacks and periodic background work.

Its public issue history also highlights recurring operational problems: startup timeouts, path quoting, self-contained and
single-file publishing, recovery configuration, delayed automatic start, multiple instances, privilege failures, and unclear
diagnostics.

## Current platform reference

The new design follows Microsoft's current guidance:

- [Create a Windows Service using BackgroundService](https://learn.microsoft.com/en-us/dotnet/core/extensions/windows-service)
- [sc.exe create reference](https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/sc-create)
- [Windows Service Control Manager API](https://learn.microsoft.com/en-us/windows/win32/services/service-control-manager)

Microsoft's Generic Host owns application lifetime, dependency injection, logging, configuration, and Windows service
integration. DotService owns deployment definitions and operational tooling around that supported runtime.

## Independent decisions

- JSON manifests replace colon-delimited command arguments.
- Async, cancellation-aware operations replace callback configuration.
- Native SCM calls replace parsing `sc.exe` output.
- Immutable definitions defensively copy caller-owned collections.
- Passwordless accounts are the only identity configuration in the first preview.
- Installation is rolled back if post-creation configuration fails.
- Hosting, command-line tooling, and packaging are separate packages.

