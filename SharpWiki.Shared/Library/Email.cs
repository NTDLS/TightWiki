using SharpWiki.Shared.Repository;
using System.Net;
using System.Net.Mail;

namespace SharpWiki.Shared.Library
{
    public static class Email
    {
        public static void Send(string emailAddress, string subject, string htmlBody)
        {
            var values = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Email");
            var smtpPassword = values.ValueAs<string>("Password");
            var smtpUsername = values.ValueAs<string>("Username");
            var smtpAddress = values.ValueAs<string>("Address");
            var smtpFromDisplayName = values.ValueAs<string>("From Display Name");
            var smtpUseSSL = values.ValueAs<bool>("Use SSL");

            int smtpPort = values.ValueAs<int>("Port");

            var smtpClient = new SmtpClient
            {
                Host = smtpAddress,
                Port = smtpPort,
                EnableSsl = smtpUseSSL,
                UseDefaultCredentials = false,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Credentials = new NetworkCredential(smtpUsername, smtpPassword)
            };

            var message = new MailMessage();
            message.From = new MailAddress(smtpUsername, smtpFromDisplayName);
            message.To.Add(emailAddress);
            message.Subject = subject;
            message.IsBodyHtml = true;
            message.Body = htmlBody;
            smtpClient.Send(message);
        }
    }
}
