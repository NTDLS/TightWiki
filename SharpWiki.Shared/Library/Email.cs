using SharpWiki.Shared.Repository;
using System.Net;
using System.Net.Mail;

namespace SharpWiki.Shared.Library
{
    public static class Email
    {
        public static void SendEmail(string emailAddress, string subject, string htmlBody)
        {
            var values = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Email");
            string smtpPassword = values.ValueAs<string>("SMTP.Password");
            string smtpUsername = values.ValueAs<string>("SMTP.Username");
            string smtpAddress = values.ValueAs<string>("SMTP.Address");
            string smtpFromDisplayName = values.ValueAs<string>("SMTP.From Display Name");
            int smtpPort = values.ValueAs<int>("SMTP.Port");

            var smtpClient = new SmtpClient
            {
                Host = smtpAddress,
                Port = smtpPort,
                EnableSsl = true,
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
