using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TightWiki.Library;
using TightWiki.Models.DataModels;
using TightWiki.Repository;
using TightWiki.Wiki;
using static TightWiki.Library.Constants;

namespace TightWiki.Controllers
{
    public class WikiControllerBase : Controller
    {
        public WikiContextState WikiContext { get; private set; } = new();

        public readonly SignInManager<IdentityUser> SignInManager;
        public readonly UserManager<IdentityUser> UserManager;

        public WikiControllerBase(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        {
            SignInManager = signInManager;
            UserManager = userManager;
        }

        #region NonAction Functions.

        [NonAction]
        public int SavePage(Page page)
        {
            bool isNewlyCreated = page.Id == 0;

            page.Id = PageRepository.SavePage(page);

            RefreshPageMetadata(this, page);

            if (isNewlyCreated)
            {
                //This will update the pageid of references that have been saved to the navigation link.
                PageRepository.UpdateSinglePageReference(page.Navigation, page.Id);
            }

            return page.Id;
        }

        [NonAction]
        public static void RefreshPageMetadata(WikiControllerBase controller, Page page)
        {
            var wikifier = new Wikifier(controller.WikiContext, page, null, controller.Request.Query, new WikiMatchType[] { WikiMatchType.Function });

            PageRepository.UpdatePageTags(page.Id, wikifier.Tags);
            PageRepository.UpdatePageProcessingInstructions(page.Id, wikifier.ProcessingInstructions);

            var pageTokens = wikifier.ParsePageTokens().Select(o => o.ToPageToken(page.Id)).ToList();

            PageRepository.SavePageSearchTokens(pageTokens);

            PageRepository.UpdatePageReferences(page.Id, wikifier.OutgoingLinks);

            WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.Page, [page.Id]));
            WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.Page, [page.Navigation]));
        }

        #endregion

        [NonAction]
        public override void OnActionExecuting(ActionExecutingContext filterContext)
            => ViewData["WikiContext"] = WikiContext.Hydrate(SignInManager, this);

        [NonAction]
        public override RedirectResult Redirect(string? url)
            => base.Redirect(url.EnsureNotNull());

        [NonAction]
        protected string? GetQueryString(string key)
            => Request.Query[key];

        [NonAction]
        protected string GetQueryString(string key, string defaultValue)
            => (string?)Request.Query[key] ?? defaultValue;

        [NonAction]
        protected int GetQueryString(string key, int defaultValue)
            => int.Parse(GetQueryString(key, defaultValue.ToString()));

        [NonAction]
        protected string? GetFormString(string key)
            => Request.Form[key];

        [NonAction]
        protected string GetFormString(string key, string defaultValue)
            => (string?)Request.Form[key] ?? defaultValue;

        [NonAction]
        protected int GetFormString(string key, int defaultValue)
            => int.Parse(GetFormString(key, defaultValue.ToString()));

        protected RedirectResult NotifyOfAction(string successMessage, string errorMessage, string redirectUrl)
            => Redirect($"/Utility/NotifyWithRedirectCountdown?SuccessMessage={successMessage}&ErrorMessage={errorMessage}&RedirectUrl={redirectUrl}");

        protected RedirectResult NotifyOfSuccessAction(string message, string redirectUrl)
            => Redirect($"/Utility/NotifyWithRedirectCountdown?SuccessMessage={message}&RedirectUrl={redirectUrl}");

        protected RedirectResult NotifyOfErrorAction(string message, string redirectUrl)
            => Redirect($"/Utility/NotifyWithRedirectCountdown?ErrorMessage={message}&RedirectUrl={redirectUrl}");

        protected RedirectResult Notify(string successMessage, string errorMessage)
            => Redirect($"/Utility/Notify?SuccessMessage={successMessage}&ErrorMessage={errorMessage}");

        protected RedirectResult NotifyOfSuccess(string message)
            => Redirect($"/Utility/Notify?SuccessMessage={message}");

        protected RedirectResult NotifyOfError(string message)
            => Redirect($"/Utility/Notify?ErrorMessage={message}");
    }
}
