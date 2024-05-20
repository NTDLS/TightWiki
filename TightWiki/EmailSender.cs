using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using TightWiki.Library.Repository;

namespace TightWiki
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string htmlMessage);
    }

    public class SmtpSettings
    {
        public string Server { get; set; }
        public int Port { get; set; }
        public string SenderName { get; set; }
        public string SenderEmail { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class EmailSender : IEmailSender
    {
        private readonly SmtpSettings _smtpSettings;

        public EmailSender(IOptions<SmtpSettings> smtpSettings)
        {
            _smtpSettings = smtpSettings.Value;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var values = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Email");
            var smtpPassword = values.As<string>("Password");
            var smtpUsername = values.As<string>("Username");
            var smtpAddress = values.As<string>("Address");
            var smtpFromDisplayName = values.As<string>("From Display Name");
            var smtpUseSSL = values.As<bool>("Use SSL");
            int smtpPort = values.As<int>("Port");

            if (string.IsNullOrEmpty(smtpAddress) || string.IsNullOrEmpty(smtpUsername))
            {
                return;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(smtpFromDisplayName, smtpUsername));
            message.To.Add(new MailboxAddress(email, email));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlMessage };

            using (var client = new SmtpClient())
            {
                var options = smtpUseSSL ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;
                await client.ConnectAsync(smtpAddress, smtpPort, options);
                await client.AuthenticateAsync(smtpUsername, smtpPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }
    }
}
