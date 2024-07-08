using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TightWiki.Library;

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
            => Redirect($"/Utility/NotifyAction?SuccessMessage={successMessage}&ErrorMessage={errorMessage}&RedirectUrl={redirectUrl}");

        protected RedirectResult NotifyOfSuccessAction(string message, string redirectUrl)
            => Redirect($"/Utility/NotifyAction?SuccessMessage={message}&RedirectUrl={redirectUrl}");

        protected RedirectResult NotifyOfErrorAction(string message, string redirectUrl)
            => Redirect($"/Utility/NotifyAction?ErrorMessage={message}&RedirectUrl={redirectUrl}");

        protected RedirectResult Notify(string successMessage, string errorMessage)
            => Redirect($"/Utility/Notify?SuccessMessage={successMessage}&ErrorMessage={errorMessage}");

        protected RedirectResult NotifyOfSuccess(string message)
            => Redirect($"/Utility/Notify?SuccessMessage={message}");

        protected RedirectResult NotifyOfError(string message)
            => Redirect($"/Utility/Notify?ErrorMessage={message}");
    }
}
