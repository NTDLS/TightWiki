using Microsoft.AspNetCore.Identity;

namespace TightWiki.Areas.Identity.Pages.Account
{

    public class RegistrationIsNotAllowedModel : PageModelBase
    {

        public void OnGet()
        {
        }
        public RegistrationIsNotAllowedModel(SignInManager<IdentityUser> signInManager)
                        : base(signInManager)
        {
        }
    }
}
