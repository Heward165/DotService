# Architecture

DotService separates application hosting from machine deployment.

```text
.NET application
    |
    +-- DotService.Hosting --> Microsoft Generic Host and WindowsServiceLifetime
    |
dotservice CLI
    |
    +-- DotService.Packaging --> manifest validation and diagnostics
                              --> native Service Control Manager operations
```

`DotService.Hosting` is intentionally small. Removing it and calling `AddWindowsService` directly remains a supported design.
The operational value lives in `DotService.Packaging` and the CLI.

## Installation transaction

1. Parse the versioned manifest.
2. Resolve and validate the executable path, arguments, account, dependencies, and recovery policy.
3. Run preflight diagnostics before requesting elevated machine changes.
4. Create the SCM registration with an always-quoted executable path.
5. Apply description, delayed start, and recovery configuration.
6. Delete the new registration if any configuration step fails.
7. Optionally start it and wait for the Running state.

The native manager does not invoke a command shell and does not infer success from localized text.

