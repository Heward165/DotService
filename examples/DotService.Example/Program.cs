using DotService.Example;
using DotService.Hosting;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// DotService does not replace the Generic Host. It configures Microsoft's supported Windows-service lifetime and
// keeps this executable runnable as a normal console process during development.
builder.AddDotService("DotService.Example", options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(20);
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost;
});

builder.Services.AddHostedService<HeartbeatWorker>();
await builder.Build().RunAsync();

