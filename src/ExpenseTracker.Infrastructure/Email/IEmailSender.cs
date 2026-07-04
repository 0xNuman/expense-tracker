namespace ExpenseTracker.Infrastructure.Email;

/// <summary>Abstracts outbound email delivery so the dev sink (Papercut SMTP) and prod sink (AWS SES) swap cleanly.</summary>
public interface IEmailSender
{
    /// <summary>Sends the supplied message. Throws on transport failure; never silently drops.</summary>
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}