namespace ExpenseTracker.Domain.Tests;

public class UserTests
{
    [Fact]
    public void Register_NormalizesEmailAndDefaultsToUsd()
    {
        var user = User.Register("Owner@Example.com", "Owner");

        user.Email.Should().Be("Owner@Example.com");
        user.NormalizedEmail.Should().Be("OWNER@EXAMPLE.COM");
        user.PreferredBaseCurrency.Value.Should().Be("USD");
        user.TimeZone.Should().Be("UTC");
        user.PreferredLocale.Should().Be("en-US");
        user.IsPending.Should().BeFalse();
        user.EmailConfirmed.Should().BeFalse();
        user.HasEvents.Should().BeTrue();
        user.Events.OfType<UserRegistered>().Should().ContainSingle();
    }

    [Fact]
    public void Register_DerivesDisplayNameFromEmailWhenAbsent()
    {
        var user = User.Register("alice@example.com", displayName: "   ");
        user.DisplayName.Should().Be("alice");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("a@b")]
    public void Register_InvalidEmail_Rejects(string? email)
    {
        var act = () => User.Register(email!, "Bob");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Invite_MarksUserAsPending()
    {
        var user = User.Invite("pending@example.com");
        user.IsPending.Should().BeTrue();
        user.EmailConfirmed.Should().BeFalse();
        user.Events.OfType<UserInvited>().Should().ContainSingle();
    }

    [Fact]
    public void ConfirmEmail_ClearsPendingFlag()
    {
        var user = User.Invite("pending@example.com");
        user.ConfirmEmail();
        user.IsPending.Should().BeFalse();
        user.EmailConfirmed.Should().BeTrue();
    }
}