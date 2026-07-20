using System.ComponentModel;
using DotService.Packaging;

namespace DotService.Cli;

/// <summary>Dependency-light command dispatcher for the dotservice global tool.</summary>
public static class DotServiceCommand
{
    /// <summary>Executes one CLI invocation and returns a process exit code.</summary>
    public static async Task<int> RunAsync(
        IReadOnlyList<string> arguments,
        TextWriter output,
        TextWriter error,
        IWindowsServiceManager? manager = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        if (arguments.Count == 0 || IsHelp(arguments[0]))
        {
            await WriteHelpAsync(output).ConfigureAwait(false);
            return 0;
        }

        manager ??= new WindowsServiceManager();

        try
        {
            string command = arguments[0].ToLowerInvariant();
            return command switch
            {
                "install" => await InstallAsync(arguments, manager, output, cancellationToken).ConfigureAwait(false),
                "uninstall" => await UninstallAsync(arguments, manager, output, cancellationToken).ConfigureAwait(false),
                "start" => await ControlAsync(arguments, manager.StartAsync, "started", output, cancellationToken).ConfigureAwait(false),
                "stop" => await ControlAsync(arguments, manager.StopAsync, "stopped", output, cancellationToken).ConfigureAwait(false),
                "pause" => await ControlAsync(arguments, manager.PauseAsync, "paused", output, cancellationToken).ConfigureAwait(false),
                "continue" => await ControlAsync(arguments, manager.ContinueAsync, "continued", output, cancellationToken).ConfigureAwait(false),
                "status" => await StatusAsync(arguments, manager, output, cancellationToken).ConfigureAwait(false),
                "doctor" => await DoctorAsync(arguments, output, cancellationToken).ConfigureAwait(false),
                "validate" => await ValidateAsync(arguments, output, cancellationToken).ConfigureAwait(false),
                _ => await UnknownCommandAsync(command, error).ConfigureAwait(false),
            };
        }
        catch (OperationCanceledException)
        {
            await error.WriteLineAsync("Operation canceled.").ConfigureAwait(false);
            return 130;
        }
        catch (Exception exception) when (exception is
            ArgumentException or InvalidDataException or InvalidOperationException or IOException or
            TimeoutException or Win32Exception or PlatformNotSupportedException)
        {
            await error.WriteLineAsync(exception.Message).ConfigureAwait(false);
            return 1;
        }
    }

    private static async Task<int> InstallAsync(
        IReadOnlyList<string> arguments,
        IWindowsServiceManager manager,
        TextWriter output,
        CancellationToken cancellationToken)
    {
        string manifestPath = RequireValue(arguments, 1, "install requires a manifest path.");
        DotServiceManifest manifest = await DotServiceManifest.LoadAsync(manifestPath, cancellationToken).ConfigureAwait(false);
        WindowsServiceDefinition definition = manifest.Resolve(Path.GetDirectoryName(Path.GetFullPath(manifestPath))!);
        WindowsServiceManagerOptions options = ReadOptions(arguments);

        IReadOnlyList<DotServiceDiagnostic> findings = DotServiceDoctor.Analyze(definition);
        foreach (DotServiceDiagnostic finding in findings)
        {
            await output.WriteLineAsync($"{finding.Severity.ToString().ToLowerInvariant()}: {finding.Code}: {finding.Message}").ConfigureAwait(false);
        }

        if (findings.Any(static finding => finding.Severity == DotServiceDiagnosticSeverity.Error))
        {
            return 1;
        }

        WindowsServiceSnapshot snapshot = await manager.InstallAsync(definition, options, cancellationToken).ConfigureAwait(false);
        await WriteSnapshotAsync(snapshot, output).ConfigureAwait(false);
        return 0;
    }

    private static async Task<int> UninstallAsync(
        IReadOnlyList<string> arguments,
        IWindowsServiceManager manager,
        TextWriter output,
        CancellationToken cancellationToken)
    {
        string name = RequireValue(arguments, 1, "uninstall requires a service name.");
        bool removed = await manager.UninstallAsync(name, ReadOptions(arguments), cancellationToken).ConfigureAwait(false);
        await output.WriteLineAsync(removed ? $"Removed {name}." : $"Service {name} is not installed.").ConfigureAwait(false);
        return removed ? 0 : 3;
    }

    private static async Task<int> ControlAsync(
        IReadOnlyList<string> arguments,
        Func<string, WindowsServiceManagerOptions?, CancellationToken, ValueTask<WindowsServiceSnapshot>> operation,
        string verb,
        TextWriter output,
        CancellationToken cancellationToken)
    {
        string name = RequireValue(arguments, 1, $"{arguments[0]} requires a service name.");
        WindowsServiceSnapshot snapshot = await operation(name, ReadOptions(arguments), cancellationToken).ConfigureAwait(false);
        await output.WriteLineAsync($"{name} {verb}.").ConfigureAwait(false);
        await WriteSnapshotAsync(snapshot, output).ConfigureAwait(false);
        return 0;
    }

    private static async Task<int> StatusAsync(
        IReadOnlyList<string> arguments,
        IWindowsServiceManager manager,
        TextWriter output,
        CancellationToken cancellationToken)
    {
        string name = RequireValue(arguments, 1, "status requires a service name.");
        WindowsServiceSnapshot? snapshot = await manager.QueryAsync(name, cancellationToken).ConfigureAwait(false);
        if (snapshot is null)
        {
            await output.WriteLineAsync($"Service {name} is not installed.").ConfigureAwait(false);
            return 3;
        }

        await WriteSnapshotAsync(snapshot, output).ConfigureAwait(false);
        return 0;
    }

    private static async Task<int> DoctorAsync(
        IReadOnlyList<string> arguments,
        TextWriter output,
        CancellationToken cancellationToken)
    {
        string manifestPath = RequireValue(arguments, 1, "doctor requires a manifest path.");
        DotServiceManifest manifest = await DotServiceManifest.LoadAsync(manifestPath, cancellationToken).ConfigureAwait(false);
        WindowsServiceDefinition definition = manifest.Resolve(Path.GetDirectoryName(Path.GetFullPath(manifestPath))!);
        IReadOnlyList<DotServiceDiagnostic> findings = DotServiceDoctor.Analyze(definition);

        foreach (DotServiceDiagnostic finding in findings)
        {
            await output.WriteLineAsync($"{finding.Severity.ToString().ToLowerInvariant()}: {finding.Code}: {finding.Message}").ConfigureAwait(false);
        }

        return findings.Any(static finding => finding.Severity == DotServiceDiagnosticSeverity.Error) ? 1 : 0;
    }

    private static async Task<int> ValidateAsync(
        IReadOnlyList<string> arguments,
        TextWriter output,
        CancellationToken cancellationToken)
    {
        string manifestPath = RequireValue(arguments, 1, "validate requires a manifest path.");
        DotServiceManifest manifest = await DotServiceManifest.LoadAsync(manifestPath, cancellationToken).ConfigureAwait(false);
        _ = manifest.Resolve(Path.GetDirectoryName(Path.GetFullPath(manifestPath))!);
        await output.WriteLineAsync($"Valid manifest: {Path.GetFullPath(manifestPath)}").ConfigureAwait(false);
        return 0;
    }

    private static WindowsServiceManagerOptions ReadOptions(IReadOnlyList<string> arguments)
    {
        bool replace = arguments.Contains("--replace", StringComparer.OrdinalIgnoreCase);
        bool startImmediately = !arguments.Contains("--no-start", StringComparer.OrdinalIgnoreCase);
        TimeSpan timeout = TimeSpan.FromSeconds(ReadPositiveDoubleOption(arguments, "--timeout", 30));
        return new WindowsServiceManagerOptions
        {
            ReplaceExisting = replace,
            StartImmediately = startImmediately,
            OperationTimeout = timeout,
            PollInterval = TimeSpan.FromMilliseconds(Math.Min(250, timeout.TotalMilliseconds)),
        };
    }

    private static double ReadPositiveDoubleOption(
        IReadOnlyList<string> arguments,
        string option,
        double defaultValue)
    {
        for (int index = 0; index < arguments.Count; index++)
        {
            if (!string.Equals(arguments[index], option, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (index + 1 >= arguments.Count ||
                !double.TryParse(arguments[index + 1], System.Globalization.CultureInfo.InvariantCulture, out double value) ||
                value <= 0)
            {
                throw new ArgumentException($"{option} requires a positive number of seconds.");
            }

            return value;
        }

        return defaultValue;
    }

    private static string RequireValue(IReadOnlyList<string> arguments, int index, string message) =>
        index < arguments.Count && !arguments[index].StartsWith("--", StringComparison.Ordinal)
            ? arguments[index]
            : throw new ArgumentException(message);

    private static async Task WriteSnapshotAsync(WindowsServiceSnapshot snapshot, TextWriter output)
    {
        await output.WriteLineAsync($"name: {snapshot.Name}").ConfigureAwait(false);
        await output.WriteLineAsync($"status: {snapshot.Status}").ConfigureAwait(false);
        await output.WriteLineAsync($"process-id: {snapshot.ProcessId}").ConfigureAwait(false);
        await output.WriteLineAsync($"win32-exit-code: {snapshot.Win32ExitCode}").ConfigureAwait(false);
        await output.WriteLineAsync($"service-exit-code: {snapshot.ServiceSpecificExitCode}").ConfigureAwait(false);
    }

    private static async Task<int> UnknownCommandAsync(string command, TextWriter error)
    {
        await error.WriteLineAsync($"Unknown command: {command}").ConfigureAwait(false);
        await error.WriteLineAsync("Run dotservice --help for usage.").ConfigureAwait(false);
        return 2;
    }

    private static bool IsHelp(string value) => value is "help" or "--help" or "-h";

    private static async Task WriteHelpAsync(TextWriter output)
    {
        await output.WriteLineAsync("DotService - deployment and service-management toolkit for .NET on Windows").ConfigureAwait(false);
        await output.WriteLineAsync().ConfigureAwait(false);
        await output.WriteLineAsync("Usage:").ConfigureAwait(false);
        await output.WriteLineAsync("  dotservice validate <manifest>").ConfigureAwait(false);
        await output.WriteLineAsync("  dotservice doctor <manifest>").ConfigureAwait(false);
        await output.WriteLineAsync("  dotservice install <manifest> [--replace] [--no-start] [--timeout <seconds>]").ConfigureAwait(false);
        await output.WriteLineAsync("  dotservice uninstall <name> [--timeout <seconds>]").ConfigureAwait(false);
        await output.WriteLineAsync("  dotservice start|stop|pause|continue <name> [--timeout <seconds>]").ConfigureAwait(false);
        await output.WriteLineAsync("  dotservice status <name>").ConfigureAwait(false);
    }
}
