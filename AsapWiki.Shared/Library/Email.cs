using AsapWiki.Shared.Repository;
using System;
using System.Linq;
using System.Net;
using System.Net.Mail;

namespace AsapWiki.Shared.Library
{
    public static class Email
    {
        public static void SendEmail(string emailAddress, string subject, string htmlBody)
        {
            var values = ConfigurationEntryRepository.GetConfigurationEntryValuesByGroupName("Email");

            string smtpPassword = values.Where(o => o.Name == "SMTP.Password").FirstOrDefault().Value;
            string smtpUsername = values.Where(o => o.Name == "SMTP.Username").FirstOrDefault().Value;
            string smtpAddress = values.Where(o => o.Name == "SMTP.Address").FirstOrDefault().Value;
            string smtpFromDisplayName = values.Where(o => o.Name == "SMTP.From Display Name").FirstOrDefault().Value;
            int smtpPort = Int32.Parse(values.Where(o => o.Name == "SMTP.Port").FirstOrDefault().Value);

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
