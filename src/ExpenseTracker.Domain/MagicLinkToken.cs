using System.Security.Cryptography;

namespace ExpenseTracker.Domain;

/// <summary>
/// One-time magic-link token for passwordless login.
/// The raw token is returned exactly once (via the emailed link) and stored
/// only as its SHA-256 hash to avoid plaintext-at-rest. Single-use, 15 min TTL.
/// </summary>
public sealed class MagicLinkToken : AggregateRoot
{
    /// <summary>Default time-to-live for a magic link token.</summary>
    public static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(15);

    public MagicLinkTokenId Id { get; private set; }

    /// <summary>Email the link was issued to. May belong to a known or unknown user.</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>Normalised email used for lookup.</summary>
    public string NormalizedEmail { get; private set; } = string.Empty;

    /// <summary>SHA-256 hash of the raw token (base64). Never the plaintext.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>UTC moment the token was issued.</summary>
    public DateTimeOffset IssuedAtUtc { get; private set; }

    /// <summary>UTC moment the token expires.</summary>
    public DateTimeOffset ExpiresAtUtc { get; private set; }

    /// <summary>UTC moment the token was consumed; null until used.</summary>
    public DateTimeOffset? ConsumedAtUtc { get; private set; }

    /// <summary>User id resolved from a successful verify; null for unknown emails until confirmed-create.</summary>
    public UserId? UserId { get; private set; }

    /// <summary>IP of the issuer request, for audit. Nullable when not supplied.</summary>
    public string? IssuedFromIp { get; private set; }

    private MagicLinkToken() { }

    /// <summary>
    /// Issues a new magic link token. Returns the aggregate and the raw token
    /// (base64url) that must be sent by email immediately and forgotten server-side.
    /// </summary>
    public static (MagicLinkToken Token, string RawToken) Issue(string email, DateTimeOffset nowUtc, string? issuedFromIp = null, TimeSpan? ttl = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        var normalised = email.Trim().ToUpperInvariant();
        var ttlValue = ttl ?? DefaultTtl;
        if (ttlValue <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(ttl), "TTL must be positive.");

        var rawBytes = RandomNumberGenerator.GetBytes(32);
        var rawToken = Base64UrlEncoder.Encode(rawBytes);
        var hash = SHA256.HashData(rawBytes);
        var hashB64 = Convert.ToBase64String(hash);

        var token = new MagicLinkToken
        {
            Id = MagicLinkTokenId.New(),
            Email = email.Trim(),
            NormalizedEmail = normalised,
            TokenHash = hashB64,
            IssuedAtUtc = nowUtc,
            ExpiresAtUtc = nowUtc + ttlValue,
            IssuedFromIp = issuedFromIp
        };
        token.Raise(new MagicLinkTokenIssued(token.Id, token.NormalizedEmail, token.ExpiresAtUtc, nowUtc));
        return (token, rawToken);
    }

    /// <summary>True when the token has not expired and has not yet been consumed.</summary>
    public bool IsValid(DateTimeOffset atUtc) => !ConsumedAtUtc.HasValue && atUtc < ExpiresAtUtc;

    public bool IsExpired(DateTimeOffset atUtc) => atUtc >= ExpiresAtUtc;
    public bool IsConsumed => ConsumedAtUtc.HasValue;

    /// <summary>
    /// Hashes a raw token with SHA-256 and returns the base64 representation matching <see cref="TokenHash"/>.
    /// </summary>
    public static string HashRaw(string rawToken)
    {
        if (string.IsNullOrEmpty(rawToken)) throw new ArgumentException("Raw token is required.", nameof(rawToken));
        var bytes = Base64UrlEncoder.DecodeBytes(rawToken);
        return Convert.ToBase64String(SHA256.HashData(bytes));
    }

    /// <summary>
    /// Consumes the token binding it to the resolved user. Throws if already consumed or expired.
    /// </summary>
    public void Consume(UserId userId, DateTimeOffset atUtc)
    {
        if (ConsumedAtUtc.HasValue)
            throw new InvalidOperationException("Token already consumed.");
        if (atUtc >= ExpiresAtUtc)
            throw new InvalidOperationException("Token has expired.");
        ConsumedAtUtc = atUtc;
        UserId = userId;
        Raise(new MagicLinkTokenConsumed(Id, userId, atUtc));
    }
}