using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using TightWiki.Library.Security;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Repository;

namespace TightWiki
{
    /// <summary>
    /// Authenticates requests carrying a personal API token as "Authorization: Bearer [token]".
    /// The token only identifies the user, permissions are still evaluated the same way as for a cookie sign-in.
    /// </summary>
    public class TwApiTokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "ApiToken";

        private const string BearerScheme = "Bearer";

        private readonly TwConfiguration _wikiConfiguration;
        private readonly ApiTokenRepository _apiTokenRepository;
        private readonly ITwUsersRepository _usersRepository;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public TwApiTokenAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger, UrlEncoder encoder, TwConfiguration wikiConfiguration,
            ApiTokenRepository apiTokenRepository, ITwUsersRepository usersRepository,
            UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
            : base(options, logger, encoder)
        {
            _wikiConfiguration = wikiConfiguration;
            _apiTokenRepository = apiTokenRepository;
            _usersRepository = usersRepository;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // When the API is disabled it behaves as if it does not exist, so the header is ignored.
            if (!_wikiConfiguration.EnableApi)
            {
                return AuthenticateResult.NoResult();
            }

            if (!AuthenticationHeaderValue.TryParse(Request.Headers.Authorization, out var header)
                || !string.Equals(header.Scheme, BearerScheme, StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrEmpty(header.Parameter))
            {
                return AuthenticateResult.NoResult();
            }

            var userId = await _apiTokenRepository.GetUserIdByApiTokenHash(ApiTokenUtility.HashToken(header.Parameter));
            if (userId == null)
            {
                return AuthenticateResult.Fail("Invalid API token.");
            }

            // Same checks as the cookie sign-in: the user needs a profile (see TwSessionState.HydrateSecurityContext)
            // and must not be locked out (see Login.cshtml.cs).
            var profile = await _usersRepository.GetBasicProfileByUserId(userId.Value);
            if (profile == null)
            {
                return AuthenticateResult.Fail("The user does not exist or has no profile.");
            }

            var user = await _userManager.FindByIdAsync(userId.Value.ToString());
            if (user == null || await _userManager.IsLockedOutAsync(user))
            {
                return AuthenticateResult.Fail("The user does not exist or is locked out.");
            }

            // The password sign-in also requires a confirmed account when "Require Email Verification" is on.
            // LDAP and external sign-ins do not, so an account with such a login is not held to it.
            if (!await _signInManager.CanSignInAsync(user) && (await _userManager.GetLoginsAsync(user)).Count == 0)
            {
                return AuthenticateResult.Fail("The account is not confirmed.");
            }

            var identity = new ClaimsIdentity(SchemeName);
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
            return AuthenticateResult.Success(ticket);
        }
    }
}
