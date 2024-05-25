using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TightWiki.Library;

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
            => ((string?)Request.Query[key]) ?? defaultValue;

        [NonAction]
        protected int GetQueryString(string key, int defaultValue)
            => int.Parse(GetQueryString(key, defaultValue.ToString()));

        [NonAction]
        protected string? GetFormString(string key)
            => Request.Form[key];

        [NonAction]
        protected string GetFormString(string key, string defaultValue)
            => ((string?)Request.Form[key]) ?? defaultValue;

        [NonAction]
        protected int GetFormString(string key, int defaultValue)
            => int.Parse(GetFormString(key, defaultValue.ToString()));

        protected RedirectResult NotifyAction(string successMessage, string errorMessage, string redirectUrl)
            => Redirect($"/Utility/NotifyAction?SuccessMessage={successMessage}&ErrorMessage={errorMessage}&RedirectUrl={redirectUrl}");

        protected RedirectResult NotifySuccessAction(string message, string redirectUrl)
            => Redirect($"/Utility/NotifyAction?SuccessMessage={message}&RedirectUrl={redirectUrl}");

        protected RedirectResult NotifyErrorAction(string message, string redirectUrl)
            => Redirect($"/Utility/NotifyAction?ErrorMessage={message}&RedirectUrl={redirectUrl}");
    }
}
