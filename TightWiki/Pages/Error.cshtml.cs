using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.Library;
using TightWiki.Models;

namespace TightWiki.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModelBase
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        private readonly ILogger<ITightEngine> _logger;

        public ErrorModel(ILogger<ITightEngine> logger, SignInManager<IdentityUser> signInManager,
            ISharedLocalizationText localizer, TightWikiConfiguration wikiConfiguration)
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
