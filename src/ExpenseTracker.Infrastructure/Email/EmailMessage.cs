namespace ExpenseTracker.Infrastructure.Email;

/// <summary>An outbound email message.</summary>
public sealed class EmailMessage
{
    public string To { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string HtmlBody { get; init; } = string.Empty;
    public string? TextBody { get; init; }

    public EmailMessage() { }

    public EmailMessage(string to, string subject, string htmlBody, string? textBody = null)
    {
        if (string.IsNullOrWhiteSpace(to)) throw new ArgumentException("Recipient is required.", nameof(to));
        if (string.IsNullOrWhiteSpace(subject)) throw new ArgumentException("Subject is required.", nameof(subject));
        To = to;
        Subject = subject;
        HtmlBody = htmlBody;
        TextBody = textBody;
    }
}