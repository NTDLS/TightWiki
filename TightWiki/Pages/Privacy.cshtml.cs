using Microsoft.AspNetCore.Identity;
using TightWiki.Engine.Library.Interfaces;

namespace TightWiki.Pages
{
    public class PrivacyModel : PageModelBase
    {
        private readonly ILogger<ITightEngine> _logger;

        public PrivacyModel(SignInManager<IdentityUser> signInManager, ILogger<ITightEngine> logger)
            : base(logger, signInManager)
        {
            _logger = logger;
        }

        public void OnGet()
        {
        }
    }
}
