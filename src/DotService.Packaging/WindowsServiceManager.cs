using System.ComponentModel;
using System.Runtime.InteropServices;
using DotService.Packaging.Management.Native;

namespace DotService.Packaging;

/// <summary>
/// Provides cancellation-aware lifecycle operations directly against the native Windows Service Control Manager.
/// No shell or locale-dependent command output is involved.
/// </summary>
public sealed class WindowsServiceManager : IWindowsServiceManager
{
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a manager using the system clock.</summary>
    public WindowsServiceManager()
        : this(TimeProvider.System)
    {
    }

    /// <summary>Initializes a manager with a clock that can be controlled by tests.</summary>
    public WindowsServiceManager(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public async ValueTask<WindowsServiceSnapshot> InstallAsync(
        WindowsServiceDefinition definition,
        WindowsServiceManagerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        EnsureWindows();
        ArgumentNullException.ThrowIfNull(definition);
        cancellationToken.ThrowIfCancellationRequested();

        WindowsServiceManagerOptions settings = NormalizeOptions(options);
        if (!File.Exists(definition.ExecutablePath))
        {
            throw new FileNotFoundException("The service executable does not exist.", definition.ExecutablePath);
        }

        if (settings.StartImmediately && definition.StartMode == WindowsServiceStartMode.Disabled)
        {
            throw new ArgumentException("A disabled service cannot start immediately.", nameof(options));
        }

        if (settings.ReplaceExisting)
        {
            _ = await UninstallAsync(definition.Name, settings, cancellationToken).ConfigureAwait(false);
        }

        using SafeServiceHandle manager = OpenManager(
            NativeConstants.ScManagerConnect | NativeConstants.ScManagerCreateService);

        string commandLine = WindowsCommandLine.Build(definition.ExecutablePath, definition.CopyArguments());
        string? dependencyList = BuildDependencyList(definition.CopyDependencies());
        string? accountName = ResolveAccountName(definition.Account);

        using SafeServiceHandle service = new(NativeMethods.CreateService(
            manager,
            definition.Name,
            definition.DisplayName,
            NativeConstants.ServiceAllAccess,
            NativeConstants.ServiceWin32OwnProcess,
            ResolveStartType(definition.StartMode),
            NativeConstants.ServiceErrorNormal,
            commandLine,
            loadOrderGroup: null,
            tagIdentifier: 0,
            dependencyList,
            accountName,
            password: null));

        if (service.IsInvalid)
        {
            ThrowLastError($"create service '{definition.Name}'");
        }

        bool configured = false;
        try
        {
            ConfigureDescription(service, definition.Description, definition.Name);
            if (definition.StartMode == WindowsServiceStartMode.AutomaticDelayed)
            {
                ConfigureDelayedStart(service, definition.Name);
            }

            if (definition.Recovery is not null)
            {
                ConfigureRecovery(service, definition.Recovery, definition.Name);
            }

            configured = true;
        }
        finally
        {
            // Installation is transactional with respect to configuration. A partially configured registration is
            // more dangerous than no registration, so roll it back when any ChangeServiceConfig2 call fails.
            if (!configured)
            {
                _ = NativeMethods.DeleteService(service);
            }
        }

        if (settings.StartImmediately)
        {
            return await StartWithHandleAsync(
                definition.Name,
                service,
                settings,
                cancellationToken).ConfigureAwait(false);
        }

        return QueryWithHandle(definition.Name, service);
    }

    /// <inheritdoc />
    public async ValueTask<bool> UninstallAsync(
        string serviceName,
        WindowsServiceManagerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        EnsureWindows();
        ValidateName(serviceName);
        cancellationToken.ThrowIfCancellationRequested();
        WindowsServiceManagerOptions settings = NormalizeOptions(options);

        using SafeServiceHandle manager = OpenManager(NativeConstants.ScManagerConnect);
        using SafeServiceHandle? service = TryOpenService(
            manager,
            serviceName,
            NativeConstants.Delete | NativeConstants.ServiceQueryStatus | NativeConstants.ServiceStop);

        if (service is null)
        {
            return false;
        }

        WindowsServiceSnapshot snapshot = QueryWithHandle(serviceName, service);
        if (snapshot.Status != WindowsServiceStatus.Stopped)
        {
            if (!NativeMethods.ControlService(service, NativeConstants.ServiceControlStop, out _))
            {
                int error = Marshal.GetLastPInvokeError();
                if (error != NativeConstants.ErrorServiceNotActive)
                {
                    throw CreateNativeException(error, $"stop service '{serviceName}' before uninstall");
                }
            }

            _ = await WaitForStateAsync(
                serviceName,
                service,
                WindowsServiceStatus.Stopped,
                settings,
                cancellationToken).ConfigureAwait(false);
        }

        if (!NativeMethods.DeleteService(service))
        {
            ThrowLastError($"delete service '{serviceName}'");
        }

        return true;
    }

    /// <inheritdoc />
    public async ValueTask<WindowsServiceSnapshot> StartAsync(
        string serviceName,
        WindowsServiceManagerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        EnsureWindows();
        ValidateName(serviceName);
        WindowsServiceManagerOptions settings = NormalizeOptions(options);
        using SafeServiceHandle manager = OpenManager(NativeConstants.ScManagerConnect);
        using SafeServiceHandle service = OpenServiceOrThrow(
            manager,
            serviceName,
            NativeConstants.ServiceStart | NativeConstants.ServiceQueryStatus);
        return await StartWithHandleAsync(serviceName, service, settings, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<WindowsServiceSnapshot> StopAsync(
        string serviceName,
        WindowsServiceManagerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        EnsureWindows();
        ValidateName(serviceName);
        WindowsServiceManagerOptions settings = NormalizeOptions(options);
        using SafeServiceHandle manager = OpenManager(NativeConstants.ScManagerConnect);
        using SafeServiceHandle service = OpenServiceOrThrow(
            manager,
            serviceName,
            NativeConstants.ServiceStop | NativeConstants.ServiceQueryStatus);

        WindowsServiceSnapshot snapshot = QueryWithHandle(serviceName, service);
        if (snapshot.Status == WindowsServiceStatus.Stopped)
        {
            return snapshot;
        }

        if (!NativeMethods.ControlService(service, NativeConstants.ServiceControlStop, out _))
        {
            int error = Marshal.GetLastPInvokeError();
            if (error != NativeConstants.ErrorServiceNotActive)
            {
                throw CreateNativeException(error, $"stop service '{serviceName}'");
            }
        }

        return await WaitForStateAsync(
            serviceName,
            service,
            WindowsServiceStatus.Stopped,
            settings,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<WindowsServiceSnapshot> PauseAsync(
        string serviceName,
        WindowsServiceManagerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        EnsureWindows();
        ValidateName(serviceName);
        WindowsServiceManagerOptions settings = NormalizeOptions(options);
        using SafeServiceHandle manager = OpenManager(NativeConstants.ScManagerConnect);
        using SafeServiceHandle service = OpenServiceOrThrow(
            manager,
            serviceName,
            NativeConstants.ServicePauseContinue | NativeConstants.ServiceQueryStatus);

        WindowsServiceSnapshot snapshot = QueryWithHandle(serviceName, service);
        if (snapshot.Status == WindowsServiceStatus.Paused)
        {
            return snapshot;
        }

        if (!snapshot.AcceptsPauseAndContinue)
        {
            throw new InvalidOperationException($"Service '{serviceName}' does not accept pause and continue controls.");
        }

        if (!NativeMethods.ControlService(service, NativeConstants.ServiceControlPause, out _))
        {
            ThrowLastError($"pause service '{serviceName}'");
        }

        return await WaitForStateAsync(
            serviceName,
            service,
            WindowsServiceStatus.Paused,
            settings,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<WindowsServiceSnapshot> ContinueAsync(
        string serviceName,
        WindowsServiceManagerOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        EnsureWindows();
        ValidateName(serviceName);
        WindowsServiceManagerOptions settings = NormalizeOptions(options);
        using SafeServiceHandle manager = OpenManager(NativeConstants.ScManagerConnect);
        using SafeServiceHandle service = OpenServiceOrThrow(
            manager,
            serviceName,
            NativeConstants.ServicePauseContinue | NativeConstants.ServiceQueryStatus);

        WindowsServiceSnapshot snapshot = QueryWithHandle(serviceName, service);
        if (snapshot.Status == WindowsServiceStatus.Running)
        {
            return snapshot;
        }

        if (snapshot.Status != WindowsServiceStatus.Paused)
        {
            throw new InvalidOperationException($"Service '{serviceName}' must be paused before it can continue.");
        }

        if (!NativeMethods.ControlService(service, NativeConstants.ServiceControlContinue, out _))
        {
            ThrowLastError($"continue service '{serviceName}'");
        }

        return await WaitForStateAsync(
            serviceName,
            service,
            WindowsServiceStatus.Running,
            settings,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public ValueTask<WindowsServiceSnapshot?> QueryAsync(
        string serviceName,
        CancellationToken cancellationToken = default)
    {
        EnsureWindows();
        ValidateName(serviceName);
        cancellationToken.ThrowIfCancellationRequested();

        using SafeServiceHandle manager = OpenManager(NativeConstants.ScManagerConnect);
        using SafeServiceHandle? service = TryOpenService(manager, serviceName, NativeConstants.ServiceQueryStatus);
        WindowsServiceSnapshot? snapshot = service is null ? null : QueryWithHandle(serviceName, service);
        return ValueTask.FromResult(snapshot);
    }

    private async ValueTask<WindowsServiceSnapshot> StartWithHandleAsync(
        string serviceName,
        SafeServiceHandle service,
        WindowsServiceManagerOptions options,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        WindowsServiceSnapshot snapshot = QueryWithHandle(serviceName, service);
        if (snapshot.Status == WindowsServiceStatus.Running)
        {
            return snapshot;
        }

        if (!NativeMethods.StartService(service, argumentCount: 0, arguments: 0))
        {
            int error = Marshal.GetLastPInvokeError();
            if (error != NativeConstants.ErrorServiceAlreadyRunning)
            {
                throw CreateNativeException(error, $"start service '{serviceName}'");
            }
        }

        return await WaitForStateAsync(
            serviceName,
            service,
            WindowsServiceStatus.Running,
            options,
            cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<WindowsServiceSnapshot> WaitForStateAsync(
        string serviceName,
        SafeServiceHandle service,
        WindowsServiceStatus desiredStatus,
        WindowsServiceManagerOptions options,
        CancellationToken cancellationToken)
    {
        long started = _timeProvider.GetTimestamp();
        while (_timeProvider.GetElapsedTime(started) < options.OperationTimeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WindowsServiceSnapshot snapshot = QueryWithHandle(serviceName, service);
            if (snapshot.Status == desiredStatus)
            {
                return snapshot;
            }

            if (desiredStatus == WindowsServiceStatus.Running && snapshot.Status == WindowsServiceStatus.Stopped)
            {
                throw new InvalidOperationException(
                    $"Service '{serviceName}' stopped before reaching Running. " +
                    $"Win32 exit code: {snapshot.Win32ExitCode}; service exit code: {snapshot.ServiceSpecificExitCode}.");
            }

            await Task.Delay(options.PollInterval, _timeProvider, cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException(
            $"Service '{serviceName}' did not reach {desiredStatus} within {options.OperationTimeout}.");
    }

    private static WindowsServiceSnapshot QueryWithHandle(string serviceName, SafeServiceHandle service)
    {
        if (!NativeMethods.QueryServiceStatus(
                service,
                NativeConstants.ScStatusProcessInfo,
                out NativeServiceStatusProcess status,
                Marshal.SizeOf<NativeServiceStatusProcess>(),
                out _))
        {
            ThrowLastError($"query service '{serviceName}'");
        }

        return new WindowsServiceSnapshot(
            serviceName,
            MapStatus(status.CurrentState),
            status.ProcessId,
            status.Win32ExitCode,
            status.ServiceSpecificExitCode,
            (status.ControlsAccepted & NativeConstants.ServiceAcceptStop) != 0,
            (status.ControlsAccepted & NativeConstants.ServiceAcceptPauseContinue) != 0);
    }

    private static SafeServiceHandle OpenManager(uint access)
    {
        SafeServiceHandle manager = new(NativeMethods.OpenServiceControlManager(null, null, access));
        if (manager.IsInvalid)
        {
            int error = Marshal.GetLastPInvokeError();
            manager.Dispose();
            throw CreateNativeException(error, "open the Windows Service Control Manager");
        }

        return manager;
    }

    private static SafeServiceHandle OpenServiceOrThrow(
        SafeServiceHandle manager,
        string serviceName,
        uint access)
    {
        SafeServiceHandle? service = TryOpenService(manager, serviceName, access);
        return service ?? throw CreateNativeException(
            NativeConstants.ErrorServiceDoesNotExist,
            $"open service '{serviceName}'");
    }

    private static SafeServiceHandle? TryOpenService(
        SafeServiceHandle manager,
        string serviceName,
        uint access)
    {
        SafeServiceHandle service = new(NativeMethods.OpenService(manager, serviceName, access));
        if (!service.IsInvalid)
        {
            return service;
        }

        int error = Marshal.GetLastPInvokeError();
        service.Dispose();
        if (error == NativeConstants.ErrorServiceDoesNotExist)
        {
            return null;
        }

        throw CreateNativeException(error, $"open service '{serviceName}'");
    }

    private static void ConfigureDescription(SafeServiceHandle service, string description, string serviceName)
    {
        nint text = Marshal.StringToHGlobalUni(description);
        try
        {
            ApplyConfiguration(
                service,
                NativeConstants.ServiceConfigDescription,
                new NativeServiceDescription { Description = text },
                $"set description for service '{serviceName}'");
        }
        finally
        {
            Marshal.FreeHGlobal(text);
        }
    }

    private static void ConfigureDelayedStart(SafeServiceHandle service, string serviceName) =>
        ApplyConfiguration(
            service,
            NativeConstants.ServiceConfigDelayedAutoStartInfo,
            new NativeDelayedAutoStartInfo { DelayedAutoStart = true },
            $"enable delayed start for service '{serviceName}'");

    private static void ConfigureRecovery(
        SafeServiceHandle service,
        WindowsServiceRecoveryPolicy policy,
        string serviceName)
    {
        WindowsServiceRecoveryAction[] actions = policy.CopyActions();
        int actionSize = Marshal.SizeOf<NativeServiceFailureAction>();
        nint actionsPointer = Marshal.AllocHGlobal(actionSize * actions.Length);

        try
        {
            for (int index = 0; index < actions.Length; index++)
            {
                WindowsServiceRecoveryAction action = actions[index];
                NativeServiceFailureAction nativeAction = new()
                {
                    Type = action.Type == WindowsServiceRecoveryActionType.Restart
                        ? NativeConstants.ScActionRestart
                        : NativeConstants.ScActionNone,
                    DelayMilliseconds = checked((uint)action.Delay.TotalMilliseconds),
                };
                Marshal.StructureToPtr(nativeAction, nint.Add(actionsPointer, index * actionSize), fDeleteOld: false);
            }

            NativeServiceFailureActions configuration = new()
            {
                ResetPeriodSeconds = checked((uint)policy.ResetPeriod.TotalSeconds),
                ActionCount = checked((uint)actions.Length),
                Actions = actionsPointer,
            };
            ApplyConfiguration(
                service,
                NativeConstants.ServiceConfigFailureActions,
                configuration,
                $"set recovery policy for service '{serviceName}'");
        }
        finally
        {
            Marshal.FreeHGlobal(actionsPointer);
        }

        ApplyConfiguration(
            service,
            NativeConstants.ServiceConfigFailureActionsFlag,
            new NativeFailureActionsFlag { Enabled = policy.ApplyToNonCrashFailures },
            $"set recovery failure flag for service '{serviceName}'");
    }

    private static void ApplyConfiguration<T>(
        SafeServiceHandle service,
        uint informationLevel,
        T configuration,
        string operation)
        where T : struct
    {
        nint pointer = Marshal.AllocHGlobal(Marshal.SizeOf<T>());
        try
        {
            Marshal.StructureToPtr(configuration, pointer, fDeleteOld: false);
            if (!NativeMethods.ChangeServiceConfig2(service, informationLevel, pointer))
            {
                ThrowLastError(operation);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
    }

    private static uint ResolveStartType(WindowsServiceStartMode startMode) => startMode switch
    {
        WindowsServiceStartMode.Automatic or WindowsServiceStartMode.AutomaticDelayed => NativeConstants.ServiceAutoStart,
        WindowsServiceStartMode.Manual => NativeConstants.ServiceDemandStart,
        WindowsServiceStartMode.Disabled => NativeConstants.ServiceDisabled,
        _ => throw new ArgumentOutOfRangeException(nameof(startMode)),
    };

    private static string? ResolveAccountName(WindowsServiceAccount account) => account.Kind switch
    {
        WindowsServiceAccountKind.LocalSystem => null,
        WindowsServiceAccountKind.LocalService => @"NT AUTHORITY\LocalService",
        WindowsServiceAccountKind.NetworkService => @"NT AUTHORITY\NetworkService",
        WindowsServiceAccountKind.Managed => account.AccountName,
        _ => throw new ArgumentOutOfRangeException(nameof(account)),
    };

    private static string? BuildDependencyList(string[] dependencies) =>
        dependencies.Length == 0 ? null : string.Join('\0', dependencies) + "\0\0";

    private static WindowsServiceStatus MapStatus(uint status) => status switch
    {
        NativeConstants.ServiceStopped => WindowsServiceStatus.Stopped,
        NativeConstants.ServiceStartPending => WindowsServiceStatus.StartPending,
        NativeConstants.ServiceStopPending => WindowsServiceStatus.StopPending,
        NativeConstants.ServiceRunning => WindowsServiceStatus.Running,
        NativeConstants.ServiceContinuePending => WindowsServiceStatus.ContinuePending,
        NativeConstants.ServicePausePending => WindowsServiceStatus.PausePending,
        NativeConstants.ServicePaused => WindowsServiceStatus.Paused,
        _ => throw new InvalidDataException($"The Service Control Manager returned unknown state {status}."),
    };

    private static WindowsServiceManagerOptions NormalizeOptions(WindowsServiceManagerOptions? options)
    {
        WindowsServiceManagerOptions result = options ?? new WindowsServiceManagerOptions();
        result.Validate();
        return result;
    }

    private static void ValidateName(string serviceName) =>
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

    private static void EnsureWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("DotService service-management operations require Windows.");
        }
    }

    private static void ThrowLastError(string operation) =>
        throw CreateNativeException(Marshal.GetLastPInvokeError(), operation);

    private static Win32Exception CreateNativeException(int error, string operation) =>
        new(error, $"Failed to {operation}. Windows error {error}.");
}
