namespace ExpenseTracker.Domain;

/// <summary>
/// A user of the application. Email is the canonical identifier.
/// A user may belong to many tenants (workspaces) via <see cref="TenantMembership"/> rows.
/// Commercial preferences (base currency, locale, tz) live here; per-tenant data lives on <see cref="Tenant"/>.
/// </summary>
public sealed class User : AggregateRoot
{
    public UserId Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string NormalizedEmail { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>Per-user preference for aggregations and reports. Default: USD.</summary>
    public CurrencyCode PreferredBaseCurrency { get; private set; }

    /// <summary>IANA timezone id, e.g. 'America/Chicago'. Default: 'UTC'.</summary>
    public string TimeZone { get; private set; } = "UTC";

    /// <summary>BCP-47 locale tag, e.g. 'en-US'.</summary>
    public string PreferredLocale { get; private set; } = "en-US";

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? LastLoginAtUtc { get; private set; }

    /// <summary>True when the user record is created via invitation but not yet claimed.</summary>
    public bool IsPending { get; private set; }

    /// <summary>True once the user has verified ownership of their email (magic-link confirmation).</summary>
    public bool EmailConfirmed { get; private set; }

    private User() { }

    public static User Register(string email, string displayName, CurrencyCode? baseCurrency = null)
    {
        var trimmed = ValidateEmail(email);
        var name = string.IsNullOrWhiteSpace(displayName) ? ExtractNameFromEmail(trimmed) : displayName.Trim();

        var user = new User
        {
            Id = UserId.New(),
            Email = trimmed,
            NormalizedEmail = trimmed.ToUpperInvariant(),
            DisplayName = name,
            PreferredBaseCurrency = baseCurrency ?? CurrencyCode.From("USD"),
            TimeZone = "UTC",
            PreferredLocale = "en-US",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsPending = false,
            EmailConfirmed = false
        };
        user.Raise(new UserRegistered(user.Id, user.Email, user.CreatedAtUtc));
        return user;
    }

    /// <summary>Creates a placeholder user via invitation flow that must complete signup before login.</summary>
    public static User Invite(string email)
    {
        var trimmed = ValidateEmail(email);
        var user = new User
        {
            Id = UserId.New(),
            Email = trimmed,
            NormalizedEmail = trimmed.ToUpperInvariant(),
            DisplayName = ExtractNameFromEmail(trimmed),
            PreferredBaseCurrency = CurrencyCode.From("USD"),
            TimeZone = "UTC",
            PreferredLocale = "en-US",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsPending = true,
            EmailConfirmed = false
        };
        user.Raise(new UserInvited(user.Id, user.Email, user.CreatedAtUtc));
        return user;
    }

    public void ConfirmEmail()
    {
        EmailConfirmed = true;
        IsPending = false;
    }

    public void ChangeDisplayName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) throw new ArgumentException("Display name is required.", nameof(newName));
        DisplayName = newName.Trim();
    }

    public void SetPreferredBaseCurrency(CurrencyCode currency)
    {
        PreferredBaseCurrency = currency;
    }

    public void SetTimeZone(string ianaTz)
    {
        if (string.IsNullOrWhiteSpace(ianaTz)) throw new ArgumentException("TimeZone is required.", nameof(ianaTz));
        TimeZone = ianaTz.Trim();
    }

    public void SetPreferredLocale(string locale)
    {
        if (string.IsNullOrWhiteSpace(locale)) throw new ArgumentException("PreferredLocale is required.", nameof(locale));
        PreferredLocale = locale.Trim();
    }

    public void RecordLogin(DateTimeOffset atUtc) => LastLoginAtUtc = atUtc;

    private static string ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));
        var trimmed = email.Trim();
        if (trimmed.Length > 254)
            throw new ArgumentException("Email is not a valid address.", nameof(email));
        var at = trimmed.IndexOf('@');
        if (at <= 0 || at >= trimmed.Length - 1 || trimmed[at..].IndexOf('.') < 0)
            throw new ArgumentException("Email is not a valid address.", nameof(email));
        return trimmed;
    }

    private static string ExtractNameFromEmail(string email)
    {
        var at = email.IndexOf('@');
        return at <= 0 ? email : email[..at];
    }
}