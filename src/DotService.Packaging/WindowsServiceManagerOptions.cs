namespace DotService.Packaging;

/// <summary>Controls lifecycle waiting and replacement behavior.</summary>
public sealed class WindowsServiceManagerOptions
{
    /// <summary>Gets or sets the maximum time to wait for a requested state.</summary>
    public TimeSpan OperationTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Gets or sets the interval between SCM status queries.</summary>
    public TimeSpan PollInterval { get; init; } = TimeSpan.FromMilliseconds(250);

    /// <summary>Gets or sets whether install replaces an existing registration.</summary>
    public bool ReplaceExisting { get; init; }

    /// <summary>Gets or sets whether installation starts the service after configuration succeeds.</summary>
    public bool StartImmediately { get; init; } = true;

    internal void Validate()
    {
        if (OperationTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(OperationTimeout));
        }

        if (PollInterval <= TimeSpan.Zero || PollInterval > OperationTimeout)
        {
            throw new ArgumentOutOfRangeException(nameof(PollInterval));
        }
    }
}
