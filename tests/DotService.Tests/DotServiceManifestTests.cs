using DotService.Packaging;

namespace DotService.Tests;

public sealed class DotServiceManifestTests
{
    [Fact]
    public async Task RoundTripPreservesOperationalConfiguration()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"dotservice-{Guid.NewGuid():N}");
        string path = Path.Combine(directory, "dotservice.json");
        DotServiceManifest original = new()
        {
            Name = "Acme.Worker",
            Executable = @"publish\Acme.Worker.exe",
            DisplayName = "Acme Worker",
            Description = "Processes queued work.",
            StartMode = WindowsServiceStartMode.AutomaticDelayed,
            Account = WindowsServiceAccountKind.LocalService,
            Arguments = ["--contentRoot", @"C:\ProgramData\Acme"],
            Dependencies = ["Tcpip"],
            Recovery = new DotServiceRecoveryManifest { RestartAttempts = 2 },
        };

        await original.SaveAsync(path);
        DotServiceManifest loaded = await DotServiceManifest.LoadAsync(path);
        WindowsServiceDefinition definition = loaded.Resolve(directory);

        Assert.Equal(original.Name, definition.Name);
        Assert.Equal(WindowsServiceStartMode.AutomaticDelayed, definition.StartMode);
        Assert.Equal(WindowsServiceAccountKind.LocalService, definition.Account.Kind);
        Assert.Equal(2, definition.Recovery!.Actions.Count);
        Assert.True(Path.IsPathFullyQualified(definition.ExecutablePath));
    }

    [Fact]
    public void ResolveRejectsUnknownSchema()
    {
        DotServiceManifest manifest = new()
        {
            SchemaVersion = 99,
            Name = "Acme.Worker",
            Executable = "worker.exe",
        };

        Assert.Throws<InvalidDataException>(() => manifest.Resolve(Path.GetTempPath()));
    }
}

