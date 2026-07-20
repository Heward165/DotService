namespace DotService.Packaging;

/// <summary>Installs, controls, queries, and removes Windows services.</summary>
public interface IWindowsServiceManager
{
    /// <summary>Creates and fully configures a service registration.</summary>
    ValueTask<WindowsServiceSnapshot> InstallAsync(
        WindowsServiceDefinition definition,
        WindowsServiceManagerOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Removes a registration, stopping the service first when necessary.</summary>
    ValueTask<bool> UninstallAsync(
        string serviceName,
        WindowsServiceManagerOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Starts a service and waits until it reports Running.</summary>
    ValueTask<WindowsServiceSnapshot> StartAsync(
        string serviceName,
        WindowsServiceManagerOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Stops a service and waits until it reports Stopped.</summary>
    ValueTask<WindowsServiceSnapshot> StopAsync(
        string serviceName,
        WindowsServiceManagerOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Pauses a service that advertises pause support.</summary>
    ValueTask<WindowsServiceSnapshot> PauseAsync(
        string serviceName,
        WindowsServiceManagerOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Continues a paused service.</summary>
    ValueTask<WindowsServiceSnapshot> ContinueAsync(
        string serviceName,
        WindowsServiceManagerOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the current service state, or null when no registration exists.</summary>
    ValueTask<WindowsServiceSnapshot?> QueryAsync(
        string serviceName,
        CancellationToken cancellationToken = default);
}

