using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExpenseTracker.Infrastructure.Email;

/// <summary>
/// SMTP-backed <see cref="IEmailSender"/> using the BCL SmtpClient.
/// Suitable for the dev Papercut sink. For production SES, swap in an SES implementation.
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailSenderOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailSenderOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.UseSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = string.IsNullOrEmpty(_options.Username),
            Credentials = string.IsNullOrEmpty(_options.Username)
                ? null
                : new NetworkCredential(_options.Username!, _options.Password!)
        };

        if (_options.SkipCertificateValidation)
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback = (_, _, _, _) => true;
        }

        using var mail = new MailMessage()
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = message.Subject,
            Body = message.HtmlBody,
            IsBodyHtml = true
        };
        mail.To.Add(message.To);
        if (message.TextBody is not null)
        {
            var alternate = AlternateView.CreateAlternateViewFromString(message.TextBody, null, "text/plain");
            mail.AlternateViews.Add(alternate);
        }

        try
        {
            _logger.LogInformation("Sending email to {To} subject='{Subject}' via {Host}:{Port}",
                message.To, message.Subject, _options.Host, _options.Port);
            await client.SendMailAsync(mail, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}.", message.To);
            throw;
        }
    }
}