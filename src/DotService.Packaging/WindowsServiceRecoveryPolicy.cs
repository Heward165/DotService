namespace DotService.Packaging;

/// <summary>Defines an action that the Service Control Manager takes after a process failure.</summary>
public readonly record struct WindowsServiceRecoveryAction(
    WindowsServiceRecoveryActionType Type,
    TimeSpan Delay)
{
    /// <summary>Creates a restart action.</summary>
    public static WindowsServiceRecoveryAction RestartAfter(TimeSpan delay) =>
        new(WindowsServiceRecoveryActionType.Restart, delay);

    /// <summary>Creates a no-op action.</summary>
    public static WindowsServiceRecoveryAction None =>
        new(WindowsServiceRecoveryActionType.None, TimeSpan.Zero);
}

/// <summary>Lists the supported Service Control Manager failure actions.</summary>
public enum WindowsServiceRecoveryActionType
{
    /// <summary>No action is taken.</summary>
    None,

    /// <summary>The Service Control Manager restarts the service.</summary>
    Restart,
}

/// <summary>Configures deterministic recovery behavior after service-process failures.</summary>
public sealed class WindowsServiceRecoveryPolicy
{
    private readonly WindowsServiceRecoveryAction[] _actions;

    /// <summary>Initializes a recovery policy.</summary>
    public WindowsServiceRecoveryPolicy(
        TimeSpan resetPeriod,
        IEnumerable<WindowsServiceRecoveryAction> actions,
        bool applyToNonCrashFailures = true)
    {
        ArgumentNullException.ThrowIfNull(actions);

        if (resetPeriod < TimeSpan.Zero || resetPeriod.TotalSeconds > uint.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(resetPeriod));
        }

        _actions = [.. actions];
        if (_actions.Length is < 1 or > 16)
        {
            throw new ArgumentException("A recovery policy must contain between one and sixteen actions.", nameof(actions));
        }

        foreach (WindowsServiceRecoveryAction action in _actions)
        {
            if (action.Delay < TimeSpan.Zero || action.Delay.TotalMilliseconds > uint.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(actions), "Recovery delays must fit in an unsigned 32-bit millisecond value.");
            }
        }

        ResetPeriod = resetPeriod;
        ApplyToNonCrashFailures = applyToNonCrashFailures;
    }

    /// <summary>Gets the interval after which the failure count resets.</summary>
    public TimeSpan ResetPeriod { get; }

    /// <summary>Gets whether recovery applies when a service stops with an error code without crashing.</summary>
    public bool ApplyToNonCrashFailures { get; }

    /// <summary>Gets a defensively copied snapshot of the configured actions.</summary>
    public IReadOnlyList<WindowsServiceRecoveryAction> Actions => Array.AsReadOnly(_actions);

    internal WindowsServiceRecoveryAction[] CopyActions() => [.. _actions];

    /// <summary>Creates a common restart policy with bounded retry attempts.</summary>
    public static WindowsServiceRecoveryPolicy Restart(
        int attempts = 3,
        TimeSpan? delay = null,
        TimeSpan? resetPeriod = null)
    {
        if (attempts is < 1 or > 16)
        {
            throw new ArgumentOutOfRangeException(nameof(attempts));
        }

        WindowsServiceRecoveryAction action = WindowsServiceRecoveryAction.RestartAfter(delay ?? TimeSpan.FromMinutes(1));
        return new WindowsServiceRecoveryPolicy(
            resetPeriod ?? TimeSpan.FromDays(1),
            Enumerable.Repeat(action, attempts));
    }
}
