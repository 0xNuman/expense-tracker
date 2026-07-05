using ExpenseTracker.Domain;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ExpenseTracker.Infrastructure.Persistence.Converters;

/// <summary>EF Core value converters for the Domain's strongly-typed IDs and value objects.</summary>
public static class DomainConverters
{
    /// <summary>Converts <see cref="TenantId"/> to/from <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<TenantId, Guid> TenantIdConverter =
        new(v => v.Value, g => new TenantId(g));

    /// <summary>Converts <see cref="UserId"/> to/from <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<UserId, Guid> UserIdConverter =
        new(v => v.Value, g => new UserId(g));

    /// <summary>Converts <see cref="TenantMembershipId"/> to/from <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<TenantMembershipId, Guid> TenantMembershipIdConverter =
        new(v => v.Value, g => new TenantMembershipId(g));

    /// <summary>Converts <see cref="MagicLinkTokenId"/> to/from <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<MagicLinkTokenId, Guid> MagicLinkTokenConverter =
        new(v => v.Value, g => new MagicLinkTokenId(g));

    /// <summary>Converts <see cref="RefreshTokenId"/> to/from <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<RefreshTokenId, Guid> RefreshTokenIdConverter =
        new(v => v.Value, g => new RefreshTokenId(g));

    /// <summary>Converts <see cref="PasskeyCredentialId"/> to/from <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<PasskeyCredentialId, Guid> PasskeyCredentialIdConverter =
        new(v => v.Value, g => new PasskeyCredentialId(g));

    /// <summary>Converts <see cref="RefreshTokenId"/> to/from nullable <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<RefreshTokenId?, Guid?> NullableRefreshTokenIdConverter =
        new(v => v.HasValue ? v.Value.Value : null, g => g.HasValue ? new RefreshTokenId(g.Value) : null);

    /// <summary>Converts <see cref="AccountId"/> to/from <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<AccountId, Guid> AccountIdConverter =
        new(v => v.Value, g => new AccountId(g));

    /// <summary>Converts <see cref="TransactionId"/> to/from <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<TransactionId, Guid> TransactionIdConverter =
        new(v => v.Value, g => new TransactionId(g));

    /// <summary>Converts <see cref="CategoryId"/> to/from <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<CategoryId, Guid> CategoryIdConverter =
        new(v => v.Value, g => new CategoryId(g));

    /// <summary>Converts <see cref="CategoryId"/> to/from nullable <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<CategoryId?, Guid?> NullableCategoryIdConverter =
        new(v => v.HasValue ? v.Value.Value : null, g => g.HasValue ? new CategoryId(g.Value) : null);

    /// <summary>Converts <see cref="AccountId"/> to/from nullable <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<AccountId?, Guid?> NullableAccountIdConverter =
        new(v => v.HasValue ? v.Value.Value : null, g => g.HasValue ? new AccountId(g.Value) : null);

    /// <summary>
    /// Converts <see cref="Money"/> to/from its <see cref="Money.Amount"/> decimal value.
    /// Currency is stored separately on the entity.
    /// </summary>
    public static readonly ValueConverter<Money, decimal> MoneyToAmountConverter =
        new(m => m.Amount, d => Money.Of(d, CurrencyCode.From("USD")));

    /// <summary>Converts <see cref="CurrencyCode"/> to/from its 3-letter string.</summary>
    public static readonly ValueConverter<CurrencyCode, string> CurrencyCodeConverter =
        new(c => c.Value, s => CurrencyCode.From(s));

    /// <summary>Converts <see cref="UserId"/> to/from nullable <see cref="Guid"/>.</summary>
    public static readonly ValueConverter<UserId?, Guid?> NullableUserIdConverter =
        new(v => v.HasValue ? v.Value.Value : null, g => g.HasValue ? new UserId(g.Value) : null);
}