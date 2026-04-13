using CoachingFit.Identity.Services.Abstraction;
using CoachingFit.Identity.Shared.Messages;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace CoachingFit.Identity.Infrastructure.ExternalServices
{
    public class EmailService(IOptions<EmailSettings> _options) : IEmailService
    {
        public async Task SendEmailAsync(EmailMessage email)
        {
            var mail = new MimeMessage
            {
                Sender = MailboxAddress.Parse(_options.Value.Email),
                Subject = email.Subject
            };

            mail.To.Add(MailboxAddress.Parse(email.To));
            mail.From.Add(new MailboxAddress(_options.Value.DisplayName, _options.Value.Email));

            var builder = new BodyBuilder { HtmlBody = email.Body };
            mail.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_options.Value.Host, _options.Value.Port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_options.Value.Email, _options.Value.Password);
            await smtp.SendAsync(mail);
            await smtp.DisconnectAsync(true);
        }
    }
}
