namespace DotService.Packaging.Management.Native;

internal static class NativeConstants
{
    internal const uint ScManagerConnect = 0x0001;
    internal const uint ScManagerCreateService = 0x0002;

    internal const uint Delete = 0x00010000;
    internal const uint ServiceQueryConfig = 0x0001;
    internal const uint ServiceChangeConfig = 0x0002;
    internal const uint ServiceQueryStatus = 0x0004;
    internal const uint ServiceStart = 0x0010;
    internal const uint ServiceStop = 0x0020;
    internal const uint ServicePauseContinue = 0x0040;

    internal const uint ServiceAllAccess = 0x000F01FF;
    internal const uint ServiceWin32OwnProcess = 0x00000010;
    internal const uint ServiceAutoStart = 0x00000002;
    internal const uint ServiceDemandStart = 0x00000003;
    internal const uint ServiceDisabled = 0x00000004;
    internal const uint ServiceErrorNormal = 0x00000001;

    internal const uint ServiceControlStop = 0x00000001;
    internal const uint ServiceControlPause = 0x00000002;
    internal const uint ServiceControlContinue = 0x00000003;

    internal const uint ServiceAcceptStop = 0x00000001;
    internal const uint ServiceAcceptPauseContinue = 0x00000002;

    internal const uint ServiceStopped = 0x00000001;
    internal const uint ServiceStartPending = 0x00000002;
    internal const uint ServiceStopPending = 0x00000003;
    internal const uint ServiceRunning = 0x00000004;
    internal const uint ServiceContinuePending = 0x00000005;
    internal const uint ServicePausePending = 0x00000006;
    internal const uint ServicePaused = 0x00000007;

    internal const int ScStatusProcessInfo = 0;
    internal const uint ServiceConfigDescription = 1;
    internal const uint ServiceConfigFailureActions = 2;
    internal const uint ServiceConfigDelayedAutoStartInfo = 3;
    internal const uint ServiceConfigFailureActionsFlag = 4;

    internal const uint ScActionNone = 0;
    internal const uint ScActionRestart = 1;

    internal const int ErrorServiceAlreadyRunning = 1056;
    internal const int ErrorServiceDoesNotExist = 1060;
    internal const int ErrorServiceNotActive = 1062;
    internal const int ErrorServiceExists = 1073;
}
