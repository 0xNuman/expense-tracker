namespace ExpenseTracker.Infrastructure.Email;

/// <summary>Configuration for the SMTP email sender.</summary>
public sealed class EmailSenderOptions
{
    public const string SectionName = "Email:Smtp";

    public string Host { get; init; } = "localhost";

    public int Port { get; init; } = 25;

    /// <summary>Username for authenticated SMTP; null for unauthenticated dev (Papercut).</summary>
    public string? Username { get; init; }

    public string? Password { get; init; }

    /// <summary>From address used on every outbound message.</summary>
    public string FromAddress { get; init; } = "no-reply@expensetracker.local";

    /// <summary>Display name for the From header.</summary>
    public string FromName { get; init; } = "Expense Tracker";

    /// <summary>True to use SSL/TLS on connect. Default false (Papercut/SES PIPELINE differ).</summary>
    public bool UseSsl { get; init; } = false;

    /// <summary>True to skip certificate validation (e.g. local test SMTP without a valid cert).</summary>
    public bool SkipCertificateValidation { get; init; } = true;
}