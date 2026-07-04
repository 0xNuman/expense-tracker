namespace ExpenseTracker.Infrastructure.Auth;

/// <summary>Configuration for JWT issuance and validation.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Auth:Jwt";

    /// <summary>Issuer claim (iss). Typically the API's public URL.</summary>
    public string Issuer { get; init; } = "expensetracker";

    /// <summary>Audience claim (aud). Typically the client app origin.</summary>
    public string Audience { get; init; } = "expensetracker-client";

    /// <summary>Access token time-to-live. Default 15 minutes.</summary>
    public TimeSpan AccessTokenTtl { get; init; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// PEM-encoded ECDSA P-256 private key for signing. When null, an ephemeral key
    /// is generated at process start (dev only — tokens invalidate on restart).
    /// </summary>
    public string? EcdsaPrivateKeyPem { get; init; }

    /// <summary>PEM-encoded ECDSA P-256 public key. Required when <see cref="EcdsaPrivateKeyPem"/> is set.</summary>
    public string? EcdsaPublicKeyPem { get; init; }

    /// <summary>Key id surfaced in the JWT header (kid). Used for key rotation.</summary>
    public string KeyId { get; init; } = "default";
}