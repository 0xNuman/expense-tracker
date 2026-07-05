namespace ExpenseTracker.Domain.Tests;

public class RefreshTokenTests
{
    [Fact]
    public void IssueFor_ProducesHashAndRawToken()
    {
        var now = DateTimeOffset.UtcNow;
        var userId = UserId.New();

        var (token, raw) = RefreshToken.IssueFor(userId, now, "iPhone Safari", "1.2.3.4");

        token.UserId.Should().Be(userId);
        token.TokenHash.Should().NotBe(raw);
        token.TokenHash.Should().NotBeEmpty();
        token.FamilyId.Should().NotBeEmpty();
        token.ExpiresAtUtc.Should().Be(now + RefreshToken.DefaultLifetime);
        token.IsActive(now).Should().BeTrue();
        token.DeviceLabel.Should().Be("iPhone Safari");
        token.LastSeenIp.Should().Be("1.2.3.4");
        token.HasEvents.Should().BeTrue();
    }

    [Fact]
    public void Rotate_MarksOldRevoked_AndPreservesFamily()
    {
        var now = DateTimeOffset.UtcNow;
        var userId = UserId.New();
        var (token, _) = RefreshToken.IssueFor(userId, now);
        var originalFamily = token.FamilyId;

        var (replacement, rawReplacement) = token.Rotate(now + TimeSpan.FromMinutes(1));

        token.RevokedAtUtc.Should().NotBeNull();
        token.ReplacedById.Should().Be(replacement.Id);
        token.IsActive(now + TimeSpan.FromMinutes(1)).Should().BeFalse();
        replacement.FamilyId.Should().Be(originalFamily);
        replacement.TokenHash.Should().NotBe(rawReplacement);
        replacement.IsActive(now + TimeSpan.FromMinutes(1)).Should().BeTrue();
    }

    [Fact]
    public void Rotate_RevokedToken_Rejects()
    {
        var now = DateTimeOffset.UtcNow;
        var userId = UserId.New();
        var (token, _) = RefreshToken.IssueFor(userId, now);
        token.Revoke(now + TimeSpan.FromMinutes(1));

        var act = () => token.Rotate(now + TimeSpan.FromMinutes(2));
        act.Should().Throw<InvalidOperationException>().WithMessage("*revoked*");
    }

    [Fact]
    public void Rotate_ExpiredToken_Rejects()
    {
        var now = DateTimeOffset.UtcNow;
        var userId = UserId.New();
        var (token, _) = RefreshToken.IssueFor(userId, now, lifetime: TimeSpan.FromMinutes(1));

        var act = () => token.Rotate(now + TimeSpan.FromMinutes(2));
        act.Should().Throw<InvalidOperationException>().WithMessage("*expired*");
    }

    [Fact]
    public void Revoke_Idempotent()
    {
        var now = DateTimeOffset.UtcNow;
        var userId = UserId.New();
        var (token, _) = RefreshToken.IssueFor(userId, now);

        token.Revoke(now);
        var firstRevokedAt = token.RevokedAtUtc!.Value;
        token.Revoke(now + TimeSpan.FromMinutes(1));

        token.RevokedAtUtc.Should().Be(firstRevokedAt);
    }

    [Fact]
    public void HashRaw_RoundTripsWithStoredHash()
    {
        var now = DateTimeOffset.UtcNow;
        var userId = UserId.New();
        var (token, raw) = RefreshToken.IssueFor(userId, now);

        RefreshToken.HashRaw(raw).Should().Be(token.TokenHash);
    }
}