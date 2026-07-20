using DotService.Hosting;
using Microsoft.Extensions.Hosting;

namespace DotService.Tests;

public sealed class DotServiceHostingTests
{
    [Fact]
    public async Task InteractiveHostStartsAndStopsCooperatively()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.AddDotService("DotService.Tests", options =>
            options.ShutdownTimeout = TimeSpan.FromSeconds(2));

        using IHost host = builder.Build();
        await host.StartAsync();
        await host.StopAsync();
    }

    [Fact]
    public void InvalidShutdownTimeoutFailsDuringConfiguration()
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder();

        Assert.Throws<ArgumentOutOfRangeException>(() => builder.AddDotService(
            "DotService.Tests",
            options => options.ShutdownTimeout = TimeSpan.Zero));
    }
}
