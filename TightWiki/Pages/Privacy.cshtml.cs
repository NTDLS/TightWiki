using Microsoft.AspNetCore.Identity;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.Library;
using TightWiki.Models;

namespace TightWiki.Pages
{
    public class PrivacyModel : PageModelBase
    {
        private readonly ILogger<ITightEngine> _logger;

        public PrivacyModel(SignInManager<IdentityUser> signInManager, ILogger<ITightEngine> logger,
            ISharedLocalizationText localizer, TightWikiConfiguration wikiConfiguration)
            : base(logger, signInManager, localizer, wikiConfiguration)
        {
            _logger = logger;
        }

        public void OnGet()
        {
        }
    }
}
