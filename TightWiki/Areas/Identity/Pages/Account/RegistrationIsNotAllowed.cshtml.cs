using Microsoft.AspNetCore.Identity;

namespace TightWiki.Areas.Identity.Pages.Account
{

    public class RegistrationIsNotAllowedModel : PageModelBase
    {

        public void OnGet()
        {
        }
        public RegistrationIsNotAllowedModel(ILogger<RegistrationIsNotAllowedModel> logger, SignInManager<IdentityUser> signInManager)
                        : base(logger, signInManager)
        {
        }
    }
}
