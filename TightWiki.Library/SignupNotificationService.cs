using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Repository;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Library
{
    /// <summary>
    /// Emails the administrators when a new account has signed up, if "Notify Administrators of New Signup" is enabled.
    /// A failure to send is logged and never thrown, so that it cannot get in the way of the signup.
    /// </summary>
    public class SignupNotificationService(
        ITwConfigurationRepository configurationRepository,
        ITwUsersRepository usersRepository,
        ITwEmailSender emailSender,
        ILogger<ITwEngine> logger)
    {
        /// <summary>
        /// Sends the "New Signup Notification Email" to every administrator. Call this after the account and its profile have been created.
        /// </summary>
        public async Task NotifyAdministrators(Guid newUserId)
        {
            try
            {
                var membershipConfig = await configurationRepository.GetConfigurationEntryValuesByGroupName(TwConfigGroup.Membership);
                if (membershipConfig.Value("Notify Administrators of New Signup", false) == false)
                {
                    return;
                }

                var configEmailTemplate = membershipConfig.Value<string>("Template: New Signup Notification Email");
                if (string.IsNullOrEmpty(configEmailTemplate))
                {
                    return;
                }

                var basicConfig = await configurationRepository.GetConfigurationEntryValuesByGroupName(TwConfigGroup.Basic);
                var siteName = basicConfig.Value<string>("Name");
                var address = basicConfig.Value("Address", "http://localhost").TrimEnd('/');
                var profile = await usersRepository.GetAccountProfileByUserId(newUserId);

                var emailSubject = "New account signed up";
                var emailTemplate = new StringBuilder(configEmailTemplate);
                emailTemplate.Replace("##SUBJECT##", emailSubject);
                emailTemplate.Replace("##ACCOUNTCOUNTRY##", WebUtility.HtmlEncode(profile.Country));
                emailTemplate.Replace("##ACCOUNTTIMEZONE##", WebUtility.HtmlEncode(profile.TimeZone));
                emailTemplate.Replace("##ACCOUNTLANGUAGE##", WebUtility.HtmlEncode(profile.Language));
                emailTemplate.Replace("##ACCOUNTEMAIL##", WebUtility.HtmlEncode(profile.EmailAddress));
                emailTemplate.Replace("##ACCOUNTNAME##", WebUtility.HtmlEncode(profile.AccountName));
                emailTemplate.Replace("##PERSONNAME##", WebUtility.HtmlEncode($"{profile.FirstName} {profile.LastName}".Trim()));
                emailTemplate.Replace("##ACCOUNTLINK##", $"{address}/AdminSecurity/Account/{profile.Navigation}");
                emailTemplate.Replace("##SITENAME##", siteName);
                emailTemplate.Replace("##SITEADDRESS##", address);

                foreach (var user in await usersRepository.GetAllUsers())
                {
                    if (await usersRepository.IsUserMemberOfAdministrators(user.UserId))
                    {
                        await emailSender.SendEmailAsync(user.EmailAddress, emailSubject, emailTemplate.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Failed to notify the administrators of the new signup: {Message}", ex.Message);
            }
        }
    }
}
