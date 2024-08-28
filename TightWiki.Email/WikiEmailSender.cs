using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using TightWiki.Library.Interfaces;
using TightWiki.Repository;

namespace TightWiki.Email
{
    public class WikiEmailSender : IWikiEmailSender
    {
        private readonly ILogger<WikiEmailSender> _logger;

        public WikiEmailSender(ILogger<WikiEmailSender> logger)
        {
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                var values = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Email");
                var smtpPassword = values.Value<string>("Password");
                var smtpUsername = values.Value<string>("Username");
                var smtpAddress = values.Value<string>("Address");
                var smtpFromDisplayName = values.Value<string>("From Display Name");
                var smtpUseSSL = values.Value<bool>("Use SSL");
                int smtpPort = values.Value<int>("Port");

                if (string.IsNullOrEmpty(smtpAddress) || string.IsNullOrEmpty(smtpUsername))
                {
                    return;
                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(smtpFromDisplayName, smtpUsername));
                message.To.Add(new MailboxAddress(email, email));
                message.Subject = subject;
                message.Body = new TextPart("html") { Text = htmlMessage };

                using var client = new SmtpClient();
                var options = smtpUseSSL ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;
                await client.ConnectAsync(smtpAddress, smtpPort, options);
                await client.AuthenticateAsync(smtpUsername, smtpPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                ExceptionRepository.InsertException(ex);
            }
        }
    }
}
