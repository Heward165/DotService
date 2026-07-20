namespace DotService.Packaging;

/// <summary>Point-in-time state reported by the Windows Service Control Manager.</summary>
public sealed record WindowsServiceSnapshot(
    string Name,
    WindowsServiceStatus Status,
    uint ProcessId,
    uint Win32ExitCode,
    uint ServiceSpecificExitCode,
    bool AcceptsStop,
    bool AcceptsPauseAndContinue);

/// <summary>Normalized Windows service states.</summary>
public enum WindowsServiceStatus
{
    /// <summary>The service is stopped.</summary>
    Stopped,

    /// <summary>The service is starting.</summary>
    StartPending,

    /// <summary>The service is stopping.</summary>
    StopPending,

    /// <summary>The service is running.</summary>
    Running,

    /// <summary>The service is preparing to continue.</summary>
    ContinuePending,

    /// <summary>The service is pausing.</summary>
    PausePending,

    /// <summary>The service is paused.</summary>
    Paused,
}

