namespace ExpenseTracker.Domain;

/// <summary>Type of transaction — direction of money flow.</summary>
public enum TransactionType
{
    Income = 0,
    Expense = 1
}

/// <summary>
/// A single income or expense entry against an account.
/// Amount is stored in the account's currency; voiding (not deletion) preserves audit history.
/// </summary>
public sealed class Transaction : AggregateRoot
{
    public TransactionId Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public AccountId AccountId { get; private set; }
    public CategoryId? CategoryId { get; private set; }
    public TransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public CurrencyCode Currency { get; private set; }
    public string? Memo { get; private set; }
    public DateOnly OccurredOn { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public UserId CreatedByUserId { get; private set; }
    public bool IsVoided { get; private set; }
    public DateTimeOffset? VoidedAtUtc { get; private set; }
    public ImportBatchId? ImportBatchId { get; private set; }
    public List<string> Tags { get; private set; } = new();

    private Transaction() { }

    public static Transaction Create(
        TenantId tenantId,
        AccountId accountId,
        TransactionType type,
        decimal amount,
        CurrencyCode currency,
        DateOnly occurredOn,
        UserId createdByUserId,
        CategoryId? categoryId = null,
        string? memo = null,
        ImportBatchId? importBatchId = null,
        IEnumerable<string>? tags = null)
    {
        if (amount <= 0m) throw new ArgumentException("Amount must be positive.", nameof(amount));
        if (occurredOn > DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date.AddDays(1)))
            throw new ArgumentException("Occurred-on cannot be more than 1 day in the future.", nameof(occurredOn));

        var txn = new Transaction
        {
            Id = TransactionId.New(),
            TenantId = tenantId,
            AccountId = accountId,
            Type = type,
            Amount = Math.Round(amount, 4, MidpointRounding.ToEven),
            Currency = currency,
            OccurredOn = occurredOn,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedByUserId = createdByUserId,
            CategoryId = categoryId,
            Memo = memo?.Trim(),
            ImportBatchId = importBatchId,
            Tags = tags?.ToList() ?? new List<string>(),
            IsVoided = false
        };
        txn.Raise(new TransactionCreated(txn.Id, tenantId, accountId, type, txn.Amount, currency, occurredOn, txn.CreatedAtUtc));
        return txn;
    }

    public void Void(DateTimeOffset atUtc)
    {
        if (IsVoided)
            throw new InvalidOperationException("Transaction is already voided.");
        IsVoided = true;
        VoidedAtUtc = atUtc;
        Raise(new TransactionVoided(Id, TenantId, atUtc));
    }

    public void AssignCategory(CategoryId? categoryId) => CategoryId = categoryId;

    public void UpdateMemo(string? memo) => Memo = memo;
}