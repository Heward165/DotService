using DotService.Packaging;

List<(string Name, Action Check)> checks =
[
    ("Empty arguments are quoted", () => Equal("\"\"", WindowsCommandLine.QuoteArgument(string.Empty))),
    ("Whitespace arguments are quoted", () => Equal("\"a b\"", WindowsCommandLine.QuoteArgument("a b"))),
    ("Simple arguments remain readable", () => Equal("plain", WindowsCommandLine.QuoteArgument("plain"))),
    ("Recovery attempts are bounded", () => Equal(3, WindowsServiceRecoveryPolicy.Restart(3).Actions.Count)),
    ("Service definitions reject self-dependency", RejectSelfDependency),
    ("Manifest schema is explicit", RejectUnknownSchema),
];

int failures = 0;
foreach ((string name, Action check) in checks)
{
    try
    {
        check();
        Console.WriteLine($"PASS: {name}");
    }
    catch (Exception exception)
    {
        failures++;
        Console.Error.WriteLine($"FAIL: {name}: {exception.Message}");
    }
}

Console.WriteLine($"{checks.Count - failures}/{checks.Count} conformance checks passed.");
return failures == 0 ? 0 : 1;

static void RejectSelfDependency()
{
    try
    {
        _ = new WindowsServiceDefinition(
            "Acme.Worker",
            Path.Combine(Path.GetTempPath(), "worker.exe"),
            dependencies: ["acme.worker"]);
    }
    catch (ArgumentException)
    {
        return;
    }

    throw new InvalidOperationException("Self-dependency was accepted.");
}

static void RejectUnknownSchema()
{
    DotServiceManifest manifest = new()
    {
        SchemaVersion = 2,
        Name = "Acme.Worker",
        Executable = "worker.exe",
    };

    try
    {
        _ = manifest.Resolve(Path.GetTempPath());
    }
    catch (InvalidDataException)
    {
        return;
    }

    throw new InvalidOperationException("Unknown schema was accepted.");
}

static void Equal<T>(T expected, T actual)
    where T : notnull
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected '{expected}', received '{actual}'.");
    }
}
