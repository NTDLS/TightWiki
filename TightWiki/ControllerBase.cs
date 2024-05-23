using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TightWiki.Library;
using TightWiki.Models.DataModels;
using TightWiki.Repository;
using TightWiki.Wiki;
using static TightWiki.Library.Constants;

namespace TightWiki
{
    public class ControllerBase : Controller
    {
        public WikiContextState WikiContext { get; private set; } = new();

        public readonly SignInManager<IdentityUser> SignInManager;
        public readonly UserManager<IdentityUser> UserManager;

        public ControllerBase(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        {
            SignInManager = signInManager;
            UserManager = userManager;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            ViewData["WikiContext"] = WikiContext.Hydrate(SignInManager, this);
        }

        protected int SavePage(Page page)
        {
            bool isNewlyCreated = page.Id == 0;

            page.Id = PageRepository.SavePage(page);

            RefreshPageProperties(page);

            if (isNewlyCreated)
            {
                //This will update the pageid of referenes that have been saved to the navigation link.
                PageRepository.UpdateSinglePageReference(page.Navigation, page.Id);
            }

            return page.Id;
        }

        protected void RefreshPageProperties(string pageNavigation)
        {
            var page = PageRepository.GetPageRevisionByNavigation(pageNavigation, null, false);
            if (page != null)
            {
                RefreshPageProperties(page);
            }
        }

        protected void RefreshPageProperties(Page page)
        {
            var wikifier = new Wikifier(WikiContext, page, null, Request.Query, new WikiMatchType[] { WikiMatchType.Function });
            PageRepository.UpdatePageTags(page.Id, wikifier.Tags);
            PageRepository.UpdatePageProcessingInstructions(page.Id, wikifier.ProcessingInstructions);

            var pageTokens = wikifier.ParsePageTokens().Select(o => o.ToPageToken(page.Id)).ToList();
            PageRepository.SavePageTokens(pageTokens);
            PageRepository.UpdatePageReferences(page.Id, wikifier.OutgoingLinks);
            WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.Page, [page.Navigation]));
        }

        public override RedirectResult Redirect(string? url)
        {
            return base.Redirect(url.EnsureNotNull());
        }

        protected string? GetQueryString(string key)
        {
            string? value = Request.Query[key];
            return value;
        }

        protected string GetQueryString(string key, string defaultValue)
        {
            string? value = Request.Query[key];
            return value ?? defaultValue;
        }

        protected string? GetFormString(string key)
        {
            string? value = Request.Form[key];
            return value;
        }

        protected string GetFormString(string key, string defaultValue)
        {
            string? value = Request.Form[key];
            return value ?? defaultValue;
        }
    }
}
