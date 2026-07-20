using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotService.Packaging;

/// <summary>Portable JSON description of a service installation.</summary>
public sealed class DotServiceManifest
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    /// <summary>Gets or sets the manifest schema version.</summary>
    public int SchemaVersion { get; init; } = 1;

    /// <summary>Gets or sets the stable Service Control Manager name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets or sets the executable path, relative to the manifest or absolute.</summary>
    public required string Executable { get; init; }

    /// <summary>Gets or sets the optional friendly display name.</summary>
    public string? DisplayName { get; init; }

    /// <summary>Gets or sets the optional service description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets or sets the startup mode.</summary>
    public WindowsServiceStartMode StartMode { get; init; } = WindowsServiceStartMode.Automatic;

    /// <summary>Gets or sets the account kind.</summary>
    public WindowsServiceAccountKind Account { get; init; } = WindowsServiceAccountKind.LocalSystem;

    /// <summary>Gets or sets a managed or virtual account name when <see cref="Account"/> is Managed.</summary>
    public string? AccountName { get; init; }

    /// <summary>Gets or sets arguments persisted in the service command line.</summary>
    public string[] Arguments { get; init; } = [];

    /// <summary>Gets or sets Service Control Manager dependencies.</summary>
    public string[] Dependencies { get; init; } = [];

    /// <summary>Gets or sets optional restart recovery settings.</summary>
    public DotServiceRecoveryManifest? Recovery { get; init; }

    /// <summary>Loads and validates a manifest from disk.</summary>
    public static async ValueTask<DotServiceManifest> LoadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string fullPath = Path.GetFullPath(path);

        await using FileStream stream = File.OpenRead(fullPath);
        DotServiceManifest? manifest = await JsonSerializer.DeserializeAsync<DotServiceManifest>(
            stream,
            SerializerOptions,
            cancellationToken).ConfigureAwait(false);

        if (manifest is null)
        {
            throw new InvalidDataException("The manifest did not contain a JSON object.");
        }

        _ = manifest.Resolve(Path.GetDirectoryName(fullPath)!);
        return manifest;
    }

    /// <summary>Saves the manifest using a stable, review-friendly JSON representation.</summary>
    public async ValueTask SaveAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string fullPath = Path.GetFullPath(path);
        string? parent = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(parent))
        {
            Directory.CreateDirectory(parent);
        }

        await using FileStream stream = File.Create(fullPath);
        await JsonSerializer.SerializeAsync(stream, this, SerializerOptions, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Resolves paths and converts the transport model into an immutable service definition.</summary>
    public WindowsServiceDefinition Resolve(string baseDirectory)
    {
        if (SchemaVersion != 1)
        {
            throw new InvalidDataException($"Unsupported manifest schema version {SchemaVersion}. Expected version 1.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);
        string executablePath = Path.IsPathFullyQualified(Executable)
            ? Executable
            : Path.Combine(Path.GetFullPath(baseDirectory), Executable);

        WindowsServiceAccount account = Account switch
        {
            WindowsServiceAccountKind.LocalSystem => WindowsServiceAccount.LocalSystem,
            WindowsServiceAccountKind.LocalService => WindowsServiceAccount.LocalService,
            WindowsServiceAccountKind.NetworkService => WindowsServiceAccount.NetworkService,
            WindowsServiceAccountKind.Managed => WindowsServiceAccount.Managed(
                AccountName ?? throw new InvalidDataException("accountName is required for a managed account.")),
            _ => throw new InvalidDataException($"Unsupported account kind '{Account}'."),
        };

        WindowsServiceRecoveryPolicy? recovery = Recovery?.ToPolicy();
        return new WindowsServiceDefinition(
            Name,
            executablePath,
            DisplayName,
            Description,
            StartMode,
            account,
            Arguments,
            Dependencies,
            recovery);
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web)
        {
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            WriteIndented = true,
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}

/// <summary>Serializable recovery policy used by <see cref="DotServiceManifest"/>.</summary>
public sealed class DotServiceRecoveryManifest
{
    /// <summary>Gets or sets how many automatic restarts are attempted.</summary>
    public int RestartAttempts { get; init; } = 3;

    /// <summary>Gets or sets the delay before each restart.</summary>
    public TimeSpan RestartDelay { get; init; } = TimeSpan.FromMinutes(1);

    /// <summary>Gets or sets the period after which the failure count resets.</summary>
    public TimeSpan ResetPeriod { get; init; } = TimeSpan.FromDays(1);

    /// <summary>Gets or sets whether non-crash error stops trigger recovery.</summary>
    public bool ApplyToNonCrashFailures { get; init; } = true;

    internal WindowsServiceRecoveryPolicy ToPolicy()
    {
        if (RestartAttempts is < 1 or > 16)
        {
            throw new InvalidDataException("recovery.restartAttempts must be between 1 and 16.");
        }

        WindowsServiceRecoveryAction action = WindowsServiceRecoveryAction.RestartAfter(RestartDelay);
        return new WindowsServiceRecoveryPolicy(
            ResetPeriod,
            Enumerable.Repeat(action, RestartAttempts),
            ApplyToNonCrashFailures);
    }
}

