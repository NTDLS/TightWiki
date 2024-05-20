using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TightWiki
{
    public class PageModelBase : PageModel
    {
        public WikiContextState WikiContext { get; private set; } = new();

        private readonly SignInManager<IdentityUser> SignInManager;

        public PageModelBase(SignInManager<IdentityUser> signInManager)
        {
            SignInManager = signInManager;
        }

        public override void OnPageHandlerExecuting(PageHandlerExecutingContext conltext)
        {
            ViewData["WikiContext"] = WikiContext.Hydrate(SignInManager, this);
        }
    }
}
