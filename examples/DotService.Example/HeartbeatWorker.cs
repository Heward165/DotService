namespace DotService.Example;

/// <summary>Small cancellation-aware worker used by the repository's installation example.</summary>
public sealed partial class HeartbeatWorker(ILogger<HeartbeatWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromSeconds(10));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            LogHeartbeat(logger, DateTimeOffset.UtcNow);
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "DotService example heartbeat at {Timestamp}")]
    private static partial void LogHeartbeat(ILogger logger, DateTimeOffset timestamp);
}
