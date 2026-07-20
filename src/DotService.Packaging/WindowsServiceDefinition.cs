namespace DotService.Packaging;

/// <summary>Immutable, validated configuration for one Windows service registration.</summary>
public sealed class WindowsServiceDefinition
{
    private readonly string[] _arguments;
    private readonly string[] _dependencies;

    /// <summary>Initializes a service definition and rejects unsafe or ambiguous values.</summary>
    public WindowsServiceDefinition(
        string name,
        string executablePath,
        string? displayName = null,
        string? description = null,
        WindowsServiceStartMode startMode = WindowsServiceStartMode.Automatic,
        WindowsServiceAccount? account = null,
        IEnumerable<string>? arguments = null,
        IEnumerable<string>? dependencies = null,
        WindowsServiceRecoveryPolicy? recovery = null)
    {
        Name = ValidateServiceName(name);
        ExecutablePath = ValidateExecutablePath(executablePath);
        DisplayName = ValidateDisplayName(displayName ?? Name);
        Description = ValidateDescription(description);
        StartMode = startMode;
        Account = account ?? WindowsServiceAccount.LocalSystem;
        Recovery = recovery;

        _arguments = arguments is null ? [] : [.. arguments];
        if (_arguments.Any(static argument => argument is null || argument.Contains('\0')))
        {
            throw new ArgumentException("Service arguments cannot contain null values or NUL characters.", nameof(arguments));
        }

        _dependencies = dependencies is null
            ? []
            : [.. dependencies.Select(ValidateServiceName).Distinct(StringComparer.OrdinalIgnoreCase)];

        if (_dependencies.Contains(Name, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("A service cannot depend on itself.", nameof(dependencies));
        }
    }

    /// <summary>Gets the stable Service Control Manager key name.</summary>
    public string Name { get; }

    /// <summary>Gets the friendly name displayed by Windows management tools.</summary>
    public string DisplayName { get; }

    /// <summary>Gets the optional service description.</summary>
    public string Description { get; }

    /// <summary>Gets the absolute executable path.</summary>
    public string ExecutablePath { get; }

    /// <summary>Gets the configured start behavior.</summary>
    public WindowsServiceStartMode StartMode { get; }

    /// <summary>Gets the passwordless service account.</summary>
    public WindowsServiceAccount Account { get; }

    /// <summary>Gets the optional recovery policy.</summary>
    public WindowsServiceRecoveryPolicy? Recovery { get; }

    /// <summary>Gets a read-only snapshot of arguments persisted in the service binary path.</summary>
    public IReadOnlyList<string> Arguments => Array.AsReadOnly(_arguments);

    /// <summary>Gets a read-only snapshot of required services.</summary>
    public IReadOnlyList<string> Dependencies => Array.AsReadOnly(_dependencies);

    internal string[] CopyArguments() => [.. _arguments];

    internal string[] CopyDependencies() => [.. _dependencies];

    private static string ValidateServiceName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        string value = name.Trim();
        if (value.Length > 256 || value.IndexOfAny(['/', '\\', '\0']) >= 0)
        {
            throw new ArgumentException("Service names must be at most 256 characters and cannot contain slash, backslash, or NUL characters.", nameof(name));
        }

        return value;
    }

    private static string ValidateDisplayName(string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        string value = displayName.Trim();
        if (value.Length > 256 || value.Contains('\0'))
        {
            throw new ArgumentException("Display names must be at most 256 characters and cannot contain NUL characters.", nameof(displayName));
        }

        return value;
    }

    private static string ValidateDescription(string? description)
    {
        string value = description?.Trim() ?? string.Empty;
        if (value.Contains('\0'))
        {
            throw new ArgumentException("Service descriptions cannot contain NUL characters.", nameof(description));
        }

        return value;
    }

    private static string ValidateExecutablePath(string executablePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        string value = executablePath.Trim();
        if (value.IndexOfAny(['\0', '"']) >= 0)
        {
            throw new ArgumentException("Executable paths cannot contain quote or NUL characters.", nameof(executablePath));
        }

        if (!Path.IsPathFullyQualified(value))
        {
            throw new ArgumentException("The service executable path must be absolute.", nameof(executablePath));
        }

        return Path.GetFullPath(value);
    }
}
