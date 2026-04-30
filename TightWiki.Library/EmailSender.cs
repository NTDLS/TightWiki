using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Repository;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Library
{
    public class EmailSender(
                ILogger<EmailSender> logger,
                ITwConfigurationRepository configurationRepository
            )
        : ITwEmailSender
    {
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                var values = await configurationRepository.GetConfigurationEntryValuesByGroupName(TwConfigGroup.Email);
                var smtpPassword = values.Value<string>("Password") ?? string.Empty;
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
                logger.LogError(ex, ex.Message);
            }
        }
    }
}
