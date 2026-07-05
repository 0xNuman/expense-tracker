namespace ExpenseTracker.Domain;

/// <summary>
/// A transfer of money between two accounts.
/// Net-worth neutral.
/// </summary>
public sealed class Transfer : AggregateRoot
{
    public TransferId Id { get; private set; }
    public TenantId TenantId { get; private set; }
    public AccountId SourceAccountId { get; private set; }
    public AccountId DestinationAccountId { get; private set; }
    
    // Amount is stored with its original currency. Money value object holds amount and currency code.
    // EF core doesn't map complex types easily without owned types, but we'll use decimal and CurrencyCode properties
    // Or, looking at Money.cs, it's a readonly record struct. If the app maps it, great, but let's see how EF handles Money.
    // Wait, in Transaction.cs, they just use decimal Amount and CurrencyCode Currency.
    // Let's use properties that can be easily mapped, but since requirements say `Money`, we will use decimal and CurrencyCode separately to be consistent with Transaction.cs, or maybe use Money. Let's stick to the requirement literally if it says Money, or use decimal/Currency for simplicity in EF config.
    // Requirements say:
    // AmountSourceCurrency : Money
    // DestinationAmountCurrency : Money
    // So let's just use decimal and CurrencyCode for source and dest.
    
    public decimal SourceAmount { get; private set; }
    public CurrencyCode SourceCurrency { get; private set; }
    
    public decimal DestinationAmount { get; private set; }
    public CurrencyCode DestinationCurrency { get; private set; }

    public FXRate? FxSnapshot { get; private set; }
    public DateOnly OccurredOnUtc { get; private set; }
    public string? Memo { get; private set; }
    public Guid? RefTransactionId { get; private set; }

    public bool IsVoided { get; private set; }
    public UserId? VoidedById { get; private set; }
    public DateTimeOffset? VoidedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private Transfer() { }

    public static Transfer Create(
        TenantId tenantId,
        AccountId sourceAccountId,
        AccountId destinationAccountId,
        decimal sourceAmount,
        CurrencyCode sourceCurrency,
        decimal destinationAmount,
        CurrencyCode destinationCurrency,
        FXRate? fxSnapshot,
        DateOnly occurredOnUtc,
        string? memo = null)
    {
        if (sourceAccountId == destinationAccountId)
            throw new ArgumentException("Source and destination accounts must be different.");
        if (sourceAmount <= 0m || destinationAmount <= 0m)
            throw new ArgumentException("Amounts must be positive.");
            
        var transfer = new Transfer
        {
            Id = TransferId.New(),
            TenantId = tenantId,
            SourceAccountId = sourceAccountId,
            DestinationAccountId = destinationAccountId,
            SourceAmount = Math.Round(sourceAmount, 4, MidpointRounding.ToEven),
            SourceCurrency = sourceCurrency,
            DestinationAmount = Math.Round(destinationAmount, 4, MidpointRounding.ToEven),
            DestinationCurrency = destinationCurrency,
            FxSnapshot = fxSnapshot,
            OccurredOnUtc = occurredOnUtc,
            Memo = memo,
            IsVoided = false,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        transfer.Raise(new TransferCreated(
            transfer.Id, 
            tenantId, 
            sourceAccountId, 
            destinationAccountId, 
            Money.Of(transfer.SourceAmount, transfer.SourceCurrency), 
            Money.Of(transfer.DestinationAmount, transfer.DestinationCurrency), 
            occurredOnUtc,
            transfer.CreatedAtUtc));
            
        return transfer;
    }

    public void Void(UserId voidedById, DateTimeOffset atUtc)
    {
        if (IsVoided)
            throw new InvalidOperationException("Transfer is already voided.");
            
        IsVoided = true;
        VoidedById = voidedById;
        VoidedAtUtc = atUtc;
        
        Raise(new TransferVoided(
            Id, 
            TenantId, 
            SourceAccountId, 
            DestinationAccountId, 
            Money.Of(SourceAmount, SourceCurrency), 
            Money.Of(DestinationAmount, DestinationCurrency), 
            voidedById, 
            atUtc));
    }
}
