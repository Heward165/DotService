using System.Runtime.InteropServices;

namespace DotService.Packaging.Management.Native;

internal static partial class NativeMethods
{
    [LibraryImport("advapi32.dll", EntryPoint = "OpenSCManagerW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    internal static partial nint OpenServiceControlManager(
        string? machineName,
        string? databaseName,
        uint desiredAccess);

    [LibraryImport("advapi32.dll", EntryPoint = "CreateServiceW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    internal static partial nint CreateService(
        SafeServiceHandle serviceControlManager,
        string serviceName,
        string displayName,
        uint desiredAccess,
        uint serviceType,
        uint startType,
        uint errorControl,
        string binaryPathName,
        string? loadOrderGroup,
        nint tagIdentifier,
        string? dependencies,
        string? serviceStartName,
        string? password);

    [LibraryImport("advapi32.dll", EntryPoint = "OpenServiceW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    internal static partial nint OpenService(
        SafeServiceHandle serviceControlManager,
        string serviceName,
        uint desiredAccess);

    [LibraryImport("advapi32.dll", EntryPoint = "DeleteService", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DeleteService(SafeServiceHandle service);

    [LibraryImport("advapi32.dll", EntryPoint = "StartServiceW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool StartService(SafeServiceHandle service, uint argumentCount, nint arguments);

    [LibraryImport("advapi32.dll", EntryPoint = "ControlService", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ControlService(
        SafeServiceHandle service,
        uint control,
        out NativeServiceStatus status);

    [LibraryImport("advapi32.dll", EntryPoint = "QueryServiceStatusEx", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool QueryServiceStatus(
        SafeServiceHandle service,
        int informationLevel,
        out NativeServiceStatusProcess status,
        int bufferSize,
        out int bytesNeeded);

    [LibraryImport("advapi32.dll", EntryPoint = "ChangeServiceConfig2W", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ChangeServiceConfig2(
        SafeServiceHandle service,
        uint informationLevel,
        nint information);

    [LibraryImport("advapi32.dll", EntryPoint = "CloseServiceHandle", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool CloseServiceHandle(nint serviceHandle);
}

[StructLayout(LayoutKind.Sequential)]
internal struct NativeServiceStatus
{
    internal uint ServiceType;
    internal uint CurrentState;
    internal uint ControlsAccepted;
    internal uint Win32ExitCode;
    internal uint ServiceSpecificExitCode;
    internal uint CheckPoint;
    internal uint WaitHint;
}

[StructLayout(LayoutKind.Sequential)]
internal struct NativeServiceStatusProcess
{
    internal uint ServiceType;
    internal uint CurrentState;
    internal uint ControlsAccepted;
    internal uint Win32ExitCode;
    internal uint ServiceSpecificExitCode;
    internal uint CheckPoint;
    internal uint WaitHint;
    internal uint ProcessId;
    internal uint ServiceFlags;
}

[StructLayout(LayoutKind.Sequential)]
internal struct NativeServiceDescription
{
    internal nint Description;
}

[StructLayout(LayoutKind.Sequential)]
internal struct NativeDelayedAutoStartInfo
{
    [MarshalAs(UnmanagedType.Bool)]
    internal bool DelayedAutoStart;
}

[StructLayout(LayoutKind.Sequential)]
internal struct NativeServiceFailureAction
{
    internal uint Type;
    internal uint DelayMilliseconds;
}

[StructLayout(LayoutKind.Sequential)]
internal struct NativeServiceFailureActions
{
    internal uint ResetPeriodSeconds;
    internal nint RebootMessage;
    internal nint Command;
    internal uint ActionCount;
    internal nint Actions;
}

[StructLayout(LayoutKind.Sequential)]
internal struct NativeFailureActionsFlag
{
    [MarshalAs(UnmanagedType.Bool)]
    internal bool Enabled;
}
