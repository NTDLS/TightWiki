using Microsoft.AspNetCore.Identity;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.Library;

namespace TightWiki.Pages
{
    public class PrivacyModel : PageModelBase
    {
        private readonly ILogger<ITightEngine> _logger;

        public PrivacyModel(SignInManager<IdentityUser> signInManager, ILogger<ITightEngine> logger, ISharedLocalizationText localizer)
            : base(logger, signInManager, localizer)
        {
            _logger = logger;
        }

        public void OnGet()
        {
        }
    }
}
