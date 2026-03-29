using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;

namespace TightWiki.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModelBase
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        private readonly ILogger<ITwEngine> _logger;

        public ErrorModel(ILogger<ITwEngine> logger, SignInManager<IdentityUser> signInManager,
            ITwSharedLocalizationText localizer, TwConfiguration wikiConfiguration)
            : base(logger, signInManager, localizer, wikiConfiguration)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        }
    }
}
