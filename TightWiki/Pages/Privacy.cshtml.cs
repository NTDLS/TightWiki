using Microsoft.AspNetCore.Identity;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;

namespace TightWiki.Pages
{
    public class PrivacyModel : TwPageModel
    {
        private readonly ILogger<ITwEngine> _logger;

        public PrivacyModel(SignInManager<IdentityUser> signInManager, ILogger<ITwEngine> logger,
            ITwSharedLocalizationText localizer, TwConfiguration wikiConfiguration, ITwDatabaseManager databaseManager)
            : base(logger, signInManager, localizer, wikiConfiguration, databaseManager)
        {
            _logger = logger;
        }

        public void OnGet()
        {
        }
    }
}
