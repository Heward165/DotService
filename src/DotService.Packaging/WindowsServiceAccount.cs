namespace DotService.Packaging;

/// <summary>Describes the passwordless identity assigned to a Windows service.</summary>
public sealed record WindowsServiceAccount
{
    private WindowsServiceAccount(WindowsServiceAccountKind kind, string? accountName)
    {
        Kind = kind;
        AccountName = accountName;
    }

    /// <summary>Gets the account category.</summary>
    public WindowsServiceAccountKind Kind { get; }

    /// <summary>Gets the explicit account name for a managed or virtual service account.</summary>
    public string? AccountName { get; }

    /// <summary>Runs the service with the highly privileged LocalSystem identity.</summary>
    public static WindowsServiceAccount LocalSystem { get; } = new(WindowsServiceAccountKind.LocalSystem, null);

    /// <summary>Runs the service with the low-privilege LocalService identity.</summary>
    public static WindowsServiceAccount LocalService { get; } = new(WindowsServiceAccountKind.LocalService, null);

    /// <summary>Runs the service with the NetworkService identity.</summary>
    public static WindowsServiceAccount NetworkService { get; } = new(WindowsServiceAccountKind.NetworkService, null);

    /// <summary>
    /// Uses a passwordless managed service account or virtual account. Password-bearing accounts are intentionally
    /// excluded so credentials never enter process arguments, managed strings, logs, or configuration files.
    /// </summary>
    public static WindowsServiceAccount Managed(string accountName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountName);
        if (accountName.Contains('\0'))
        {
            throw new ArgumentException("Account names cannot contain NUL characters.", nameof(accountName));
        }

        return new WindowsServiceAccount(WindowsServiceAccountKind.Managed, accountName.Trim());
    }
}

/// <summary>Identifies the supported passwordless Windows service accounts.</summary>
public enum WindowsServiceAccountKind
{
    /// <summary>The built-in LocalSystem account.</summary>
    LocalSystem,

    /// <summary>The built-in LocalService account.</summary>
    LocalService,

    /// <summary>The built-in NetworkService account.</summary>
    NetworkService,

    /// <summary>A group-managed service account or virtual service account that requires no password.</summary>
    Managed,
}
