using System.Runtime.InteropServices;
using DotService.Packaging.Management.Native;

namespace DotService.Packaging;

/// <summary>One actionable result from a pre-installation environment check.</summary>
public sealed record DotServiceDiagnostic(
    DotServiceDiagnosticSeverity Severity,
    string Code,
    string Message);

/// <summary>Severity levels returned by the DotService doctor.</summary>
public enum DotServiceDiagnosticSeverity
{
    /// <summary>Useful context that does not require action.</summary>
    Information,

    /// <summary>A risky configuration that should be reviewed.</summary>
    Warning,

    /// <summary>A condition that prevents a reliable installation.</summary>
    Error,
}

/// <summary>Runs deterministic preflight checks before DotService changes machine state.</summary>
public static class DotServiceDoctor
{
    /// <summary>Analyzes a resolved service definition and the current machine.</summary>
    public static IReadOnlyList<DotServiceDiagnostic> Analyze(WindowsServiceDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        List<DotServiceDiagnostic> findings = [];

        if (!OperatingSystem.IsWindows())
        {
            findings.Add(new(
                DotServiceDiagnosticSeverity.Error,
                "platform.windows-required",
                "Service installation and control require Windows."));
        }
        else if (!CanCreateService())
        {
            findings.Add(new(
                DotServiceDiagnosticSeverity.Error,
                "privilege.administrator-required",
                "The current process cannot create services. Run the command from an elevated terminal."));
        }

        if (!File.Exists(definition.ExecutablePath))
        {
            findings.Add(new(
                DotServiceDiagnosticSeverity.Error,
                "executable.not-found",
                $"The executable does not exist: {definition.ExecutablePath}"));
        }

        string temporaryDirectory = Path.GetFullPath(Path.GetTempPath());
        if (definition.ExecutablePath.StartsWith(temporaryDirectory, StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(new(
                DotServiceDiagnosticSeverity.Warning,
                "executable.temporary-location",
                "The executable is inside the temporary directory and may be removed without notice."));
        }

        if (definition.Account.Kind == WindowsServiceAccountKind.LocalSystem)
        {
            findings.Add(new(
                DotServiceDiagnosticSeverity.Warning,
                "account.local-system",
                "LocalSystem is highly privileged. Prefer LocalService, NetworkService, or a managed service account when possible."));
        }

        if (definition.Recovery is null)
        {
            findings.Add(new(
                DotServiceDiagnosticSeverity.Warning,
                "recovery.not-configured",
                "No recovery policy is configured; an unexpected process exit will leave the service stopped."));
        }

        if (findings.All(static finding => finding.Severity != DotServiceDiagnosticSeverity.Error))
        {
            findings.Add(new(
                DotServiceDiagnosticSeverity.Information,
                "definition.ready",
                "The manifest passed all blocking preflight checks."));
        }

        return findings.AsReadOnly();
    }

    private static bool CanCreateService()
    {
        using SafeServiceHandle manager = new(NativeMethods.OpenServiceControlManager(
            null,
            null,
            NativeConstants.ScManagerConnect | NativeConstants.ScManagerCreateService));
        if (!manager.IsInvalid)
        {
            return true;
        }

        _ = Marshal.GetLastPInvokeError();
        return false;
    }
}
