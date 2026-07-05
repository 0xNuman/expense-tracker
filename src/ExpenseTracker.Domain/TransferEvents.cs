namespace ExpenseTracker.Domain;

public record TransferCreated(
    TransferId TransferId, 
    TenantId TenantId, 
    AccountId SourceAccountId, 
    AccountId DestinationAccountId, 
    Money AmountSourceCurrency, 
    Money DestinationAmountCurrency, 
    DateOnly OccurredOnUtc,
    DateTimeOffset CreatedAtUtc) : IDomainEvent
{
    public DateTimeOffset OccurredAtUtc => CreatedAtUtc;
}

public record TransferVoided(
    TransferId TransferId, 
    TenantId TenantId, 
    AccountId SourceAccountId, 
    AccountId DestinationAccountId, 
    Money AmountSourceCurrency, 
    Money DestinationAmountCurrency, 
    UserId VoidedByUserId, 
    DateTimeOffset VoidedAtUtc) : IDomainEvent
{
    public DateTimeOffset OccurredAtUtc => VoidedAtUtc;
}
