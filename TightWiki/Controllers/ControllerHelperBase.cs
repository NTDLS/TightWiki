using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using TightWiki.Library;
using TightWiki.Library.DataModels;
using TightWiki.Library.Library;
using TightWiki.Library.Repository;
using TightWiki.Library.Wiki;
using static TightWiki.Library.Wiki.Constants;

namespace TightWiki.Controllers
{
    public class ControllerHelperBase : Controller
    {
        public StateContext context = new();

        public readonly SignInManager<IdentityUser> SignInManager;
        public readonly UserManager<IdentityUser> UserManager;

        public ControllerHelperBase(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        {
            SignInManager = signInManager;
            UserManager = userManager;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            Configure();
        }

        protected void Configure()
        {
            HydrateSecurityContext();

            context.PathAndQuery = Request.GetEncodedPathAndQuery();
            context.PageNavigation = RouteValue("givenCanonical", "Home");
            context.PageRevision = RouteValue("pageRevision");
            context.Title = GlobalSettings.Name; //Default the title to the name. This will be replaced when the page is found and loaded.

            ViewData["Context"] = context;
        }

        protected string RouteValue(string key, string defaultValue = "")
        {
            if (RouteData.Values.ContainsKey(key))
            {
                return RouteData.Values[key]?.ToString() ?? defaultValue;
            }
            return defaultValue;
        }

        protected void HydrateSecurityContext()
        {
            context.IsAuthenticated = false;

            if (SignInManager.IsSignedIn(User))
            {
                try
                {
                    string emailAddress = (User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value).EnsureNotNull();

                    context.IsAuthenticated = User.Identity?.IsAuthenticated == true;
                    if (context.IsAuthenticated)
                    {
                        var userId = Guid.Parse((User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value).EnsureNotNull());

                        context.User = ProfileRepository.GetBasicProfileByUserId(userId);
                        context.Role = (User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value?.ToString()).EnsureNotNull();
                    }
                }
                catch
                {
                    HttpContext.SignOutAsync();
                    if (User.Identity != null)
                    {
                        HttpContext.SignOutAsync(User.Identity.AuthenticationType);
                    }
                    throw;
                }
            }
        }

        protected int SavePage(Page page)
        {
            bool isNewlyCreated = (page.Id == 0);

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
            var wikifier = new Wikifier(context, page, null, Request.Query, new WikiMatchType[] { WikiMatchType.Function });
            PageTagRepository.UpdatePageTags(page.Id, wikifier.Tags);
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
