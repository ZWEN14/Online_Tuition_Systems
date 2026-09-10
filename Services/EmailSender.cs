using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace AnywhereEdureach.Services;

public sealed class SmtpOptions
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    public string From { get; set; } = "";
}

public interface IEmailSender
{
    Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default);
}

public sealed class SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly SmtpOptions options = options.Value;

    public async Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.Host) ||
            string.IsNullOrWhiteSpace(options.UserName) ||
            string.IsNullOrWhiteSpace(options.Password) ||
            string.IsNullOrWhiteSpace(options.From))
        {
            logger.LogError("SMTP is incomplete. Email to {Recipient} was not sent.", recipient);
            throw new InvalidOperationException("SMTP configuration is incomplete.");
        }

        using var client = new SmtpClient(options.Host, options.Port)
        {
            EnableSsl = options.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(options.UserName, options.Password)
        };
        using var message = new MailMessage(options.From, recipient, subject, body);
        await client.SendMailAsync(message, cancellationToken);
    }
}
