namespace ExpenseTracker.Domain.Tests;

public class MoneyTests
{
    private static readonly CurrencyCode Usd = CurrencyCode.From("USD");
    private static readonly CurrencyCode Eur = CurrencyCode.From("EUR");
    private static readonly CurrencyCode Jpy = CurrencyCode.From("JPY");

    [Fact]
    public void Of_RoundsToFourDecimalPlacesWithBankersRounding()
    {
        // MidpointRounding.ToEven (banker's rounding) chooses the even last digit at midpoints.
        // 12.12345 rounds down to 12.1234 (4 is even); 12.12355 rounds up to 12.1236 (6 is even).
        Assert.Equal(12.1234m, Money.Of(12.12345m, Usd).Amount);
        Assert.Equal(12.1236m, Money.Of(12.12355m, Usd).Amount);
        Money.Of(0m, Usd).Currency.Should().Be(Usd);
    }

    [Fact]
    public void Add_SameCurrency_Sums()
    {
        var result = Money.Of(10m, Usd).Add(Money.Of(2.5m, Usd));
        result.Amount.Should().Be(12.5m);
        result.Currency.Should().Be(Usd);
    }

    [Fact]
    public void Add_DifferentCurrency_Rejects()
    {
        var act = () => Money.Of(10m, Usd).Add(Money.Of(1m, Eur));
        act.Should().Throw<InvalidOperationException>().WithMessage("*combine*");
    }

    [Fact]
    public void ConvertTo_ProducesMoneyInTargetCurrency()
    {
        var rate = FXRate.Of(Usd, Eur, 0.92m, DateTimeOffset.UtcNow, "Manual");
        var converted = Money.Of(10m, Usd).ConvertTo(Eur, rate);
        converted.Currency.Should().Be(Eur);
        converted.Amount.Should().Be(9.2m);
    }

    [Fact]
    public void ConvertTo_WrongFromCurrency_Rejects()
    {
        var rate = FXRate.Of(Eur, Usd, 1.08m, DateTimeOffset.UtcNow, "Manual");
        var act = () => Money.Of(10m, Jpy).ConvertTo(Usd, rate);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void FXRate_Invert_ProducesInverseRate()
    {
        var rate = FXRate.Of(Usd, Eur, 0.92m, DateTimeOffset.UtcNow, "Manual");
        var inverse = rate.Invert();
        inverse.FromCurrency.Should().Be(Eur);
        inverse.ToCurrency.Should().Be(Usd);
        inverse.Convert(100m).Should().BeApproximately(108.6957m, 0.001m);
    }
}