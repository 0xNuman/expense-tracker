namespace ExpenseTracker.Domain.Tests;

public class MagicLinkTokenTests
{
    [Fact]
    public void Issue_ReturnsRawTokenAndHashedStorage()
    {
        var now = DateTimeOffset.UtcNow;

        var (token, raw) = MagicLinkToken.Issue("Owner@Example.com", now);

        token.Email.Should().Be("Owner@Example.com");
        token.NormalizedEmail.Should().Be("OWNER@EXAMPLE.COM");
        token.TokenHash.Should().NotBeEmpty();
        token.TokenHash.Should().NotBe(raw);
        token.ExpiresAtUtc.Should().Be(now + MagicLinkToken.DefaultTtl);
        token.IsConsumed.Should().BeFalse();
        token.IsValid(now).Should().BeTrue();
        token.HasEvents.Should().BeTrue();
    }

    [Fact]
    public void Issue_BlankEmail_Rejects()
    {
        var act = () => MagicLinkToken.Issue("   ", DateTimeOffset.UtcNow);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void HashRaw_RoundTripsWithStoredHash()
    {
        var (token, raw) = MagicLinkToken.Issue("user@example.com", DateTimeOffset.UtcNow);

        MagicLinkToken.HashRaw(raw).Should().Be(token.TokenHash);
    }

    [Fact]
    public void Consume_SucceedsOnce_ThenRejects()
    {
        var now = DateTimeOffset.UtcNow;
        var (token, _) = MagicLinkToken.Issue("user@example.com", now);
        var userId = UserId.New();

        token.Consume(userId, now);

        token.IsConsumed.Should().BeTrue();
        token.UserId.Should().Be(userId);

        var act = () => token.Consume(userId, now);
        act.Should().Throw<InvalidOperationException>().WithMessage("*already consumed*");
    }

    [Fact]
    public void Consume_AfterExpiry_Rejects()
    {
        var now = DateTimeOffset.UtcNow;
        var (token, _) = MagicLinkToken.Issue("user@example.com", now);
        var userId = UserId.New();
        var later = now + MagicLinkToken.DefaultTtl + TimeSpan.FromSeconds(1);

        var act = () => token.Consume(userId, later);
        act.Should().Throw<InvalidOperationException>().WithMessage("*expired*");
    }

    [Fact]
    public void Issue_NegativeTtl_Rejects()
    {
        var act = () => MagicLinkToken.Issue("user@example.com", DateTimeOffset.UtcNow, ttl: TimeSpan.FromMinutes(-5));
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}