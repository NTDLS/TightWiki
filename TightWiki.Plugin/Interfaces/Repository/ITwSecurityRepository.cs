using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace TightWiki.Plugin.Interfaces.Repository
{
    public interface ISecurityRepository
    {
        /// <summary>
        /// Detect whether this is the first time the WIKI has ever been run and do some initialization.
        /// Adds the first user with the email and password contained in Constants.DEFAULTUSERNAME and Constants.DEFAULTPASSWORD
        /// </summary>
        void ValidateEncryptionAndCreateAdminUser(UserManager<IdentityUser> userManager);
        Task UpsertUserClaims(UserManager<IdentityUser> userManager, IdentityUser user, List<Claim> givenClaims);
    }
}
