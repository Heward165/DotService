using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DotService.Hosting;

/// <summary>Connects the .NET Generic Host to the Windows service lifetime without replacing Microsoft's runtime.</summary>
public static class DotServiceHostExtensions
{
    /// <summary>
    /// Adds Microsoft's Windows service lifetime and production-safe host defaults. The same executable remains a
    /// normal console application when launched interactively, which keeps local development and debugging simple.
    /// </summary>
    public static IHostApplicationBuilder AddDotService(
        this IHostApplicationBuilder builder,
        string serviceName,
        Action<DotServiceHostingOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        DotServiceHostingOptions options = new();
        configure?.Invoke(options);
        options.Validate();

        builder.Services.AddWindowsService(windowsOptions => windowsOptions.ServiceName = serviceName.Trim());
        builder.Services.Configure<HostOptions>(hostOptions =>
        {
            hostOptions.ShutdownTimeout = options.ShutdownTimeout;
            hostOptions.BackgroundServiceExceptionBehavior = options.BackgroundServiceExceptionBehavior;
        });

        return builder;
    }
}
