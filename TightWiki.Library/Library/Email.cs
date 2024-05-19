using System;
using System.Net;
using System.Net.Mail;
using TightWiki.Library.Repository;

namespace TightWiki.Library.Library
{
    public static class Email
    {
        public static void Send(string emailAddress, string subject, string htmlBody)
        {
            try
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