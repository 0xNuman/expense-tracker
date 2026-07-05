namespace ExpenseTracker.Domain;

/// <summary>Type of account — determines balance semantics and UI grouping.</summary>
public enum AccountType
{
    Cash = 0,
    Checking = 1,
    Savings = 2,
    CreditCard = 3,
    Prepaid = 4,
    Envelope = 5
}

/// <summary>
/// An account — a pool of money (wallet, bank, credit card, envelope).
/// Balances are derived from transactions and transfers, not stored denormalised.
/// </summary>
public sealed class Account : AggregateRoot
{
    public AccountId Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public AccountType Type { get; private set; }
    public CurrencyCode Currency { get; private set; }
    public Money OpeningBalance { get; private set; }
    public DateTimeOffset OpenedAtUtc { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }
    public bool IsArchived { get; private set; }

    private Account() { }

    public static Account Create(TenantId tenantId, string name, AccountType type, CurrencyCode currency, decimal openingBalance = 0m, DateTimeOffset? openedAtUtc = null)
    {
        ValidateName(name);
        var now = DateTimeOffset.UtcNow;
        var account = new Account
        {
            Id = AccountId.New(),
            TenantId = tenantId,
            Name = name.Trim(),
            Type = type,
            Currency = currency,
            OpeningBalance = Money.Of(openingBalance, currency),
            OpenedAtUtc = openedAtUtc ?? now,
            IsArchived = false
        };
        account.Raise(new AccountCreated(account.Id, tenantId, account.Name, now));
        return account;
    }

    public void Rename(string name)
    {
        ValidateName(name);
        Name = name.Trim();
    }

    public void Archive() => IsArchived = true;
    public void Restore() => IsArchived = false;

    public void Close(DateTimeOffset asOfUtc)
    {
        if (ClosedAtUtc.HasValue) return;
        ClosedAtUtc = asOfUtc;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Account name is required.", nameof(name));
        if (name.Length > 60)
            throw new ArgumentException("Account name must be ≤ 60 chars.", nameof(name));
    }
}