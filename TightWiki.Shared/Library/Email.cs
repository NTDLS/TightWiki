using System;
using System.Net;
using System.Net.Mail;
using TightWiki.Shared.Repository;

namespace TightWiki.Shared.Library
{
    public static class Email
    {
        public static void Send(string emailAddress, string subject, string htmlBody)
        {
            try
            {
                var values = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Email");
                var smtpPassword = values.As<string>("SMTP: Password");
                var smtpUsername = values.As<string>("SMTP: Username");
                var smtpAddress = values.As<string>("SMTP: Address");
                var smtpFromDisplayName = values.As<string>("SMTP: From Display Name");
                var smtpUseSSL = values.As<bool>("SMTP: Use SSL");

                int smtpPort = values.As<int>("SMTP: Port");

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
            catch (Exception ex)
            {
                ExceptionRepository.InsertException(ex, "Failed to sent email.");
            }
        }
    }
}