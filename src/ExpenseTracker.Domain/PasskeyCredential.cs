namespace ExpenseTracker.Domain;

/// <summary>
/// A registered WebAuthn (passkey) credential for a user. Not an aggregate
/// root — passkey credentials are user-scoped, not tenant-scoped, and do not
/// raise domain events.
/// </summary>
public sealed class PasskeyCredential
{
    public PasskeyCredentialId Id { get; set; }
    public UserId UserId { get; set; }
    public string CredentialIdBase64Url { get; set; } = string.Empty;
    public byte[] PublicKey { get; set; } = Array.Empty<byte>();
    public uint SignCount { get; set; }
    public string DeviceLabel { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? LastUsedAtUtc { get; set; }
}