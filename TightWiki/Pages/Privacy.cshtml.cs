using Microsoft.AspNetCore.Identity;

namespace TightWiki.Pages
{
    public class PrivacyModel : PageModelBase
    {
        private readonly ILogger<PrivacyModel> _logger;

        public PrivacyModel(SignInManager<IdentityUser> signInManager, ILogger<PrivacyModel> logger)
            : base(signInManager)
        {
            _logger = logger;
        }

        public void OnGet()
        {
        }
    }
}
