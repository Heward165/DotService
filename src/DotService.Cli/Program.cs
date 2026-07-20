using DotService.Cli;

using CancellationTokenSource shutdown = new();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    shutdown.Cancel();
};

return await DotServiceCommand.RunAsync(args, Console.Out, Console.Error, cancellationToken: shutdown.Token);
