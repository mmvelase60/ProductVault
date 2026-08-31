namespace ProductVault.Services;

public interface IApplicationEmailSender
{
    Task SendAsync(string recipientEmail, string subject, string htmlBody, string textBody, CancellationToken cancellationToken = default);
}
