using Microsoft.AspNetCore.Identity;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.Library;

namespace TightWiki.Areas.Identity.Pages.Account
{

    public class RegistrationIsNotAllowedModel : PageModelBase
    {

        public void OnGet()
        {
        }
        public RegistrationIsNotAllowedModel(ILogger<ITightEngine> logger,
            SignInManager<IdentityUser> signInManager, ISharedLocalizationText localizer)
                        : base(logger, signInManager, localizer)
        {
        }
    }
}
