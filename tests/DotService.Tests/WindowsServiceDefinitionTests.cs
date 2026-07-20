using DotService.Packaging;

namespace DotService.Tests;

public sealed class WindowsServiceDefinitionTests
{
    [Fact]
    public void ConstructorDefensivelyCopiesCollections()
    {
        string[] arguments = ["--port", "8080"];
        string[] dependencies = ["Tcpip"];
        WindowsServiceDefinition definition = new(
            "Acme.Worker",
            TestExecutablePath(),
            arguments: arguments,
            dependencies: dependencies);

        arguments[0] = "--changed";
        dependencies[0] = "Changed";

        Assert.Equal("--port", definition.Arguments[0]);
        Assert.Equal("Tcpip", definition.Dependencies[0]);
    }

    [Fact]
    public void ConstructorRejectsSelfDependency()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new WindowsServiceDefinition(
            "Acme.Worker",
            TestExecutablePath(),
            dependencies: ["acme.worker"]));

        Assert.Contains("depend on itself", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ConstructorDeduplicatesDependenciesCaseInsensitively()
    {
        WindowsServiceDefinition definition = new(
            "Acme.Worker",
            TestExecutablePath(),
            dependencies: ["Tcpip", "TCPIP", "Dnscache"]);

        Assert.Equal(2, definition.Dependencies.Count);
    }

    [Fact]
    public void ConstructorRejectsArgumentsThatWindowsWouldTruncate()
    {
        Assert.Throws<ArgumentException>(() => new WindowsServiceDefinition(
            "Acme.Worker",
            TestExecutablePath(),
            arguments: ["safe", "unsafe\0tail"]));
    }

    private static string TestExecutablePath() => Path.Combine(Path.GetTempPath(), "Acme.Worker.exe");
}
