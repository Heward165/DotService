using Microsoft.Extensions.Hosting;

namespace DotService.Hosting;

/// <summary>Opinionated host-lifetime settings for a deployed Windows service.</summary>
public sealed class DotServiceHostingOptions
{
    /// <summary>Gets or sets how long the host gives background work to stop cooperatively.</summary>
    public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Gets or sets what the host does when a background service throws.</summary>
    public BackgroundServiceExceptionBehavior BackgroundServiceExceptionBehavior { get; set; } =
        BackgroundServiceExceptionBehavior.StopHost;

    internal void Validate()
    {
        if (ShutdownTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(ShutdownTimeout));
        }
    }
}

