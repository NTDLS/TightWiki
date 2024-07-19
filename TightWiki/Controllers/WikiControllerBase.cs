using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NTDLS.Helpers;

namespace TightWiki.Controllers
{
    public class WikiControllerBase(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        : Controller
    {
        public WikiContextState WikiContext { get; private set; } = new();

        public readonly SignInManager<IdentityUser> SignInManager = signInManager;
        public readonly UserManager<IdentityUser> UserManager = userManager;

        [NonAction]
        public override void OnActionExecuting(ActionExecutingContext filterContext)
            => ViewData["WikiContext"] = WikiContext.Hydrate(SignInManager, this);

        [NonAction]
        public override RedirectResult Redirect(string? url)
            => base.Redirect(url.EnsureNotNull());

        [NonAction]
        protected string? GetQueryValue(string key)
            => Request.Query[key];

        [NonAction]
        protected string GetQueryValue(string key, string defaultValue)
            => (string?)Request.Query[key] ?? defaultValue;

        [NonAction]
        protected int GetQueryValue(string key, int defaultValue)
            => int.Parse(GetQueryValue(key, defaultValue.ToString()));

        [NonAction]
        protected string? GetFormValue(string key)
            => Request.Form[key];

        [NonAction]
        protected string GetFormValue(string key, string defaultValue)
            => (string?)Request.Form[key] ?? defaultValue;

        [NonAction]
        protected int GetFormValue(string key, int defaultValue)
            => int.Parse(GetFormValue(key, defaultValue.ToString()));

        /// <summary>
        /// Displays the successMessage unless the errorMessage is present.
        /// </summary>
        /// <returns></returns>
        protected RedirectResult NotifyOf(string successMessage, string errorMessage, string redirectUrl)
            => Redirect($"/Utility/Notify?SuccessMessage={Uri.EscapeDataString(successMessage)}&ErrorMessage={Uri.EscapeDataString(errorMessage)}&RedirectUrl={redirectUrl}&RedirectTimeout=5");

        protected RedirectResult NotifyOfSuccess(string message, string redirectUrl)
            => Redirect($"/Utility/Notify?SuccessMessage={Uri.EscapeDataString(message)}&RedirectUrl={Uri.EscapeDataString(redirectUrl)}&RedirectTimeout=5");

        protected RedirectResult NotifyOfError(string message, string redirectUrl)
            => Redirect($"/Utility/Notify?ErrorMessage={Uri.EscapeDataString(message)}&RedirectUrl={Uri.EscapeDataString(redirectUrl)}");

        protected RedirectResult NotifyOf(string successMessage, string errorMessage)
            => Redirect($"/Utility/Notify?SuccessMessage={Uri.EscapeDataString(successMessage)}&ErrorMessage={Uri.EscapeDataString(errorMessage)}&RedirectTimeout=5");

        protected RedirectResult NotifyOfSuccess(string message)
            => Redirect($"/Utility/Notify?SuccessMessage={Uri.EscapeDataString(message)}");

        protected RedirectResult NotifyOfError(string message)
            => Redirect($"/Utility/Notify?ErrorMessage={Uri.EscapeDataString(message)}");
    }
}
