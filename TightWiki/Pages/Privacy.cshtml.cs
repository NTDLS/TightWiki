using Microsoft.AspNetCore.Identity;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;

namespace TightWiki.Pages
{
    public class PrivacyModel : PageModelBase
    {
        private readonly ILogger<ITwEngine> _logger;

        public PrivacyModel(SignInManager<IdentityUser> signInManager, ILogger<ITwEngine> logger,
            ISharedLocalizationText localizer, TwConfiguration wikiConfiguration)
            : base(logger, signInManager, localizer, wikiConfiguration)
        {
            _logger = logger;
        }

        public void OnGet()
        {
        }
    }
}
