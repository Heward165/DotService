using Microsoft.Win32.SafeHandles;

namespace DotService.Packaging.Management.Native;

internal sealed class SafeServiceHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    internal SafeServiceHandle()
        : base(ownsHandle: true)
    {
    }

    internal SafeServiceHandle(nint nativeHandle)
        : base(ownsHandle: true)
    {
        SetHandle(nativeHandle);
    }

    protected override bool ReleaseHandle() => NativeMethods.CloseServiceHandle(handle);
}
