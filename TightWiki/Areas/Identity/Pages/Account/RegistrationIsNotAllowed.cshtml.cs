using Microsoft.AspNetCore.Identity;
using TightWiki.Engine.Library.Interfaces;

namespace TightWiki.Areas.Identity.Pages.Account
{

    public class RegistrationIsNotAllowedModel : PageModelBase
    {

        public void OnGet()
        {
        }
        public RegistrationIsNotAllowedModel(ILogger<ITightEngine> logger, SignInManager<IdentityUser> signInManager)
                        : base(logger, signInManager)
        {
        }
    }
}
