namespace DotService.Packaging;

/// <summary>Controls when the Windows Service Control Manager starts a service.</summary>
public enum WindowsServiceStartMode
{
    /// <summary>The service starts during system startup.</summary>
    Automatic,

    /// <summary>The service starts automatically after the initial startup load has settled.</summary>
    AutomaticDelayed,

    /// <summary>The service starts only when explicitly requested.</summary>
    Manual,

    /// <summary>The service cannot start until its configuration changes.</summary>
    Disabled,
}
