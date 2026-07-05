using Fido2NetLib;

namespace ExpenseTracker.Infrastructure.Auth;

/// <summary>Configuration for the Fido2 (WebAuthn) relying party.</summary>
public sealed class Fido2Options
{
    public const string SectionName = "Fido2";

    /// <summary>Relying party identifier (WebAuthn RP ID, a domain). Defaults to <c>localhost</c>.</summary>
    public string ServerDomain { get; set; } = "localhost";

    /// <summary>Human-readable relying party name.</summary>
    public string ServerName { get; set; } = "Expense Tracker";

    /// <summary>Origin allowlist (e.g. <c>https://localhost:5173</c>). Dev default included.</summary>
    public IReadOnlyList<string> Origins { get; set; } = new[] { "http://localhost:5173" };

    /// <summary>Timeout (ms) for credential creation/assertion requests.</summary>
    public uint Timeout { get; set; } = 60_000;
}

/// <summary>Factory that builds the Fido2 configuration and the <see cref="IFido2"/> singleton.</summary>
public static class Fido2Setup
{
    public static IFido2 Create(Fido2Options options)
    {
        var config = new Fido2Configuration
        {
            ServerDomain = options.ServerDomain,
            ServerName = options.ServerName,
            Timeout = options.Timeout,
            Origins = new HashSet<string>(options.Origins, StringComparer.OrdinalIgnoreCase)
        };
        return new Fido2(config, metadataService: null);
    }
}