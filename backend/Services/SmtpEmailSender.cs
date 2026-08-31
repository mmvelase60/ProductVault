using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ProductVault.Services;

public sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IApplicationEmailSender
{
    public async Task SendAsync(string recipientEmail, string subject, string htmlBody, string textBody, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Username) || string.IsNullOrWhiteSpace(settings.Password))
            throw new InvalidOperationException("Email SMTP credentials are not configured. Set Email:Username and Email:Password with User Secrets.");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(settings.FromName, string.IsNullOrWhiteSpace(settings.FromAddress) ? settings.Username : settings.FromAddress));
        message.To.Add(MailboxAddress.Parse(recipientEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody, TextBody = textBody }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(settings.Host, settings.Port, SecureSocketOptions.StartTls, cancellationToken);
        await client.AuthenticateAsync(settings.Username, settings.Password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
