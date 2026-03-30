using Microsoft.AspNetCore.Identity;
using NTDLS.Helpers;
using System.Security.Claims;
using TightWiki.Plugin;
using TightWiki.Plugin.Library;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Repository
{
    public static class SecurityRepository
    {
        /// <summary>
        /// Detect whether this is the first time the WIKI has ever been run and do some initialization.
        /// Adds the first user with the email and password contained in Constants.DEFAULTUSERNAME and Constants.DEFAULTPASSWORD
        /// </summary>
        public static async void ValidateEncryptionAndCreateAdminUser(UserManager<IdentityUser> userManager)
        {
            if (await ConfigurationRepository.IsFirstRun())
            {
                //If this is the first time the app has run on this machine (based on an encryption key) then clear the admin password status.
                //This will cause the application to set the admin password to the default password and display a warning until it is changed.
                await UsersRepository.SetAdminPasswordClear();
            }

            if (await UsersRepository.AdminPasswordStatus() == WikiAdminPasswordChangeState.NeedsToBeSet)
            {
                var user = await userManager.FindByNameAsync(TwConstants.DEFAULTUSERNAME);
                if (user == null)
                {
                    var creationResult = await userManager.CreateAsync(new IdentityUser(TwConstants.DEFAULTUSERNAME), TwConstants.DEFAULTPASSWORD);
                    if (!creationResult.Succeeded)
                    {
                        throw new Exception(string.Join("\r\n", creationResult.Errors.Select(o => o.Description)));
                    }

                    user = await userManager.FindByNameAsync(TwConstants.DEFAULTUSERNAME);
                }

                user.EnsureNotNull();

                user.Email = TwConstants.DEFAULTUSERNAME; // Ensure email is set or updated
                user.EmailConfirmed = true;
                var emailUpdateResult = await userManager.UpdateAsync(user);
                if (!emailUpdateResult.Succeeded)
                {
                    throw new Exception(string.Join("\r\n", emailUpdateResult.Errors.Select(o => o.Description)));
                }

                var membershipConfig = await ConfigurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.Membership);

                var claimsToAdd = new List<Claim>
                    {
                        new (ClaimTypes.Role, "Administrator"),
                        new ("timezone", membershipConfig.Value<string>("Default TimeZone").EnsureNotNull()),
                        new (ClaimTypes.Country, membershipConfig.Value<string>("Default Country").EnsureNotNull()),
                        new ("language", membershipConfig.Value<string>("Default Language").EnsureNotNull()),
                    };

                await UpsertUserClaims(userManager, user, claimsToAdd);

                var token = await userManager.GeneratePasswordResetTokenAsync(user.EnsureNotNull());
                var result = await userManager.ResetPasswordAsync(user, token, TwConstants.DEFAULTPASSWORD);
                if (!result.Succeeded)
                {
                    throw new Exception(string.Join("\r\n", emailUpdateResult.Errors.Select(o => o.Description)));
                }

                await UsersRepository.SetAdminPasswordIsDefault();

                var existingProfileUserId = UsersRepository.GetUserAccountIdByNavigation(TwNavigation.Clean(TwConstants.DEFAULTACCOUNT));
                if (existingProfileUserId == null)
                {
                    await UsersRepository.CreateProfile(Guid.Parse(user.Id), TwConstants.DEFAULTACCOUNT);
                }
                else
                {
                    await UsersRepository.SetProfileUserId(TwConstants.DEFAULTACCOUNT, Guid.Parse(user.Id));
                }
            }
        }

        public static async Task UpsertUserClaims(UserManager<IdentityUser> userManager, IdentityUser user, List<Claim> givenClaims)
        {
            // Get existing claims for the user
            var existingClaims = await userManager.GetClaimsAsync(user);

            foreach (var givenClaim in givenClaims)
            {
                // Remove existing claims if they exist
                var firstNameClaim = existingClaims.FirstOrDefault(c => c.Type == givenClaim.Type);
                if (firstNameClaim != null)
                {
                    await userManager.RemoveClaimAsync(user, firstNameClaim);
                }

                // Add new claim.
                await userManager.AddClaimAsync(user, givenClaim);
            }

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                throw new Exception(string.Join("<br />\r\n", result.Errors.Select(o => o.Description)));
            }
        }
    }
}
