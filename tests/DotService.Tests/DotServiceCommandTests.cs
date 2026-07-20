using DotService.Cli;
using DotService.Packaging;

namespace DotService.Tests;

public sealed class DotServiceCommandTests
{
    [Fact]
    public async Task HelpIsAvailableWithoutWindowsOrElevation()
    {
        StringWriter output = new();
        StringWriter error = new();

        int exitCode = await DotServiceCommand.RunAsync(["--help"], output, error);

        Assert.Equal(0, exitCode);
        Assert.Contains("dotservice install", output.ToString(), StringComparison.Ordinal);
        Assert.Equal(string.Empty, error.ToString());
    }

    [Fact]
    public async Task StatusUsesInjectedManagerAndStructuredOutput()
    {
        StringWriter output = new();
        StringWriter error = new();
        FakeManager manager = new(new WindowsServiceSnapshot(
            "Acme.Worker",
            WindowsServiceStatus.Running,
            42,
            0,
            0,
            true,
            false));

        int exitCode = await DotServiceCommand.RunAsync(
            ["status", "Acme.Worker"],
            output,
            error,
            manager: manager);

        Assert.Equal(0, exitCode);
        Assert.Contains("status: Running", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("process-id: 42", output.ToString(), StringComparison.Ordinal);
    }

    private sealed class FakeManager(WindowsServiceSnapshot? snapshot) : IWindowsServiceManager
    {
        public ValueTask<WindowsServiceSnapshot> InstallAsync(WindowsServiceDefinition definition, WindowsServiceManagerOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<bool> UninstallAsync(string serviceName, WindowsServiceManagerOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<WindowsServiceSnapshot> StartAsync(string serviceName, WindowsServiceManagerOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<WindowsServiceSnapshot> StopAsync(string serviceName, WindowsServiceManagerOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<WindowsServiceSnapshot> PauseAsync(string serviceName, WindowsServiceManagerOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<WindowsServiceSnapshot> ContinueAsync(string serviceName, WindowsServiceManagerOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<WindowsServiceSnapshot?> QueryAsync(string serviceName, CancellationToken cancellationToken = default) => ValueTask.FromResult(snapshot);
    }
}

