using System.Security.Cryptography;

namespace ExpenseTracker.Domain;

/// <summary>
/// Refresh token for an authenticated session, rotating on each refresh.
/// Tokens come in families: all descendants of an original issue share a
/// <see cref="FamilyId"/>. When a token is reused after rotation, the entire
/// family is revoked (reuse-detection).
/// </summary>
public sealed class RefreshToken : AggregateRoot
{
    public static readonly TimeSpan DefaultLifetime = TimeSpan.FromDays(30);

    public RefreshTokenId Id { get; private set; }

    /// <summary>User this token belongs to.</summary>
    public UserId UserId { get; private set; }

    /// <summary>SHA-256 hash of the raw token.</summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>Family id — ties a chain of rotated tokens together.</summary>
    public Guid FamilyId { get; private set; }

    /// <summary>UTC moment the token was issued.</summary>
    public DateTimeOffset IssuedAtUtc { get; private set; }

    /// <summary>UTC moment the token expires.</summary>
    public DateTimeOffset ExpiresAtUtc { get; private set; }

    /// <summary>UTC moment the token was revoked (rotated or family-compromised).</summary>
    public DateTimeOffset? RevokedAtUtc { get; private set; }

    /// <summary>Replacement token id assigned on rotation; null until rotated.</summary>
    public RefreshTokenId? ReplacedById { get; private set; }

    /// <summary>Optional device label supplied at issue (e.g. "iPhone Safari").</summary>
    public string? DeviceLabel { get; private set; }

    /// <summary>Last seen IP at issue or refresh.</summary>
    public string? LastSeenIp { get; private set; }

    /// <summary>Last seen UTC at issue or refresh.</summary>
    public DateTimeOffset LastSeenAtUtc { get; private set; }

    /// <summary>True if revoked (including family-revoke) or expired.</summary>
    public bool IsRevokedOrExpired(DateTimeOffset atUtc) => RevokedAtUtc.HasValue || atUtc >= ExpiresAtUtc;

    public bool IsActive(DateTimeOffset atUtc) => !RevokedAtUtc.HasValue && atUtc < ExpiresAtUtc;

    private RefreshToken() { }

    /// <summary>
    /// Issues a new refresh token, starting a new family.
    /// Returns the aggregate and the raw token string (base64url) to set in a cookie.
    /// </summary>
    public static (RefreshToken Token, string RawToken) IssueFor(UserId userId, DateTimeOffset nowUtc, string? deviceLabel = null, string? ip = null, TimeSpan? lifetime = null)
    {
        var lifetimeValue = lifetime ?? DefaultLifetime;
        var rawBytes = RandomNumberGenerator.GetBytes(32);
        var rawToken = Base64UrlEncoder.Encode(rawBytes);
        var hash = Convert.ToBase64String(SHA256.HashData(rawBytes));

        var token = new RefreshToken
        {
            Id = RefreshTokenId.New(),
            UserId = userId,
            TokenHash = hash,
            FamilyId = Guid.NewGuid(),
            IssuedAtUtc = nowUtc,
            ExpiresAtUtc = nowUtc + lifetimeValue,
            DeviceLabel = deviceLabel,
            LastSeenIp = ip,
            LastSeenAtUtc = nowUtc
        };
        token.Raise(new RefreshTokenIssued(token.Id, userId, token.FamilyId, nowUtc));
        return (token, rawToken);
    }

    /// <summary>
    /// Rotates this token, producing a new refresh token in the same family.
    /// Marks this one as revoked and records its successor. Returns the new raw token.
    /// </summary>
    public (RefreshToken Replacement, string RawToken) Rotate(DateTimeOffset nowUtc, string? ip = null)
    {
        if (RevokedAtUtc.HasValue)
            throw new InvalidOperationException("Cannot rotate a revoked refresh token.");
        if (nowUtc >= ExpiresAtUtc)
            throw new InvalidOperationException("Cannot rotate an expired refresh token.");

        var rawBytes = RandomNumberGenerator.GetBytes(32);
        var rawToken = Base64UrlEncoder.Encode(rawBytes);
        var hash = Convert.ToBase64String(SHA256.HashData(rawBytes));

        var replacement = new RefreshToken
        {
            Id = RefreshTokenId.New(),
            UserId = UserId,
            TokenHash = hash,
            FamilyId = FamilyId,
            IssuedAtUtc = nowUtc,
            ExpiresAtUtc = nowUtc + (ExpiresAtUtc - IssuedAtUtc), // preserve remaining lifetime window
            DeviceLabel = DeviceLabel,
            LastSeenIp = ip ?? LastSeenIp,
            LastSeenAtUtc = nowUtc
        };

        RevokedAtUtc = nowUtc;
        ReplacedById = replacement.Id;
        replacement.Raise(new RefreshTokenRotated(replacement.Id, Id, UserId, FamilyId, nowUtc));
        return (replacement, rawToken);
    }

    /// <summary>
    /// Revokes this token explicitly (logout, user-initiated, reuse-detection).
    /// </summary>
    public void Revoke(DateTimeOffset atUtc)
    {
        if (RevokedAtUtc.HasValue) return;
        RevokedAtUtc = atUtc;
        Raise(new RefreshTokenRevoked(Id, UserId, FamilyId, atUtc));
    }

    /// <summary>Marks reuse-detection: call to revoke without events (caller raises family-revoke event).</summary>
    internal void MarkReused(DateTimeOffset atUtc) => RevokedAtUtc ??= atUtc;

    /// <summary>Hashes a raw token to compare with stored hashes.</summary>
    public static string HashRaw(string rawToken)
    {
        if (string.IsNullOrEmpty(rawToken)) throw new ArgumentException("Raw token is required.", nameof(rawToken));
        var bytes = Base64UrlEncoder.DecodeBytes(rawToken);
        return Convert.ToBase64String(SHA256.HashData(bytes));
    }

    public void RecordActivity(DateTimeOffset atUtc, string? ip)
    {
        LastSeenAtUtc = atUtc;
        if (!string.IsNullOrEmpty(ip)) LastSeenIp = ip;
    }
}