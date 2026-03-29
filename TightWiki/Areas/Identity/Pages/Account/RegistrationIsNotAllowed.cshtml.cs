using Microsoft.AspNetCore.Identity;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;

namespace TightWiki.Areas.Identity.Pages.Account
{

    public class RegistrationIsNotAllowedModel : PageModelBase
    {

        public void OnGet()
        {
        }
        public RegistrationIsNotAllowedModel(ILogger<ITwEngine> logger,
            SignInManager<IdentityUser> signInManager, ITwSharedLocalizationText localizer, TwConfiguration wikiConfiguration)
                        : base(logger, signInManager, localizer, wikiConfiguration)
        {
        }
    }
}
