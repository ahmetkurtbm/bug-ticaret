using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Tezgah.Notifications.Email;

public class SmtpEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string bodyHtml, CancellationToken ct)
    {
        var smtp = options.Value;
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(smtp.FromName, smtp.From));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = bodyHtml }.ToMessageBody();

        using var client = new SmtpClient { Timeout = 15_000 };
        await client.ConnectAsync(smtp.Host, smtp.Port, SecureSocketOptions.Auto, ct);
        await client.SendAsync(new SendRequest(message), ct);
        await client.DisconnectAsync(true, ct);
    }
}
