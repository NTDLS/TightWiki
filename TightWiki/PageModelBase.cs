using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TightWiki.Models;

namespace TightWiki
{
    public class PageModelBase : PageModel
    {
        public SessionState SessionState { get; private set; } = new();
        public SignInManager<IdentityUser> SignInManager { get; private set; }

        public string SuccessMessage { get; set; } = string.Empty;
        public string WarningMessage { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        public PageModelBase(SignInManager<IdentityUser> signInManager)
        {
            SignInManager = signInManager;
        }

        public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
        {
            ViewData["SessionState"] = SessionState.Hydrate(SignInManager, this);
        }

        /*
        [NonAction]
        public override RedirectResult Redirect(string? url)
            => base.Redirect(url.EnsureNotNull());
        */

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

        protected RedirectResult NotifyOfSuccess(string message, string redirectUrl)
            => Redirect($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifySuccessMessage={Uri.EscapeDataString(message)}&RedirectUrl={Uri.EscapeDataString($"{GlobalConfiguration.BasePath}{redirectUrl}")}&RedirectTimeout=5");

        protected RedirectResult NotifyOfWarning(string message, string redirectUrl)
            => Redirect($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifyWarningMessage={Uri.EscapeDataString(message)}&RedirectUrl={Uri.EscapeDataString(Uri.EscapeDataString($"{GlobalConfiguration.BasePath}{redirectUrl}"))}");

        protected RedirectResult NotifyOfError(string message, string redirectUrl)
            => Redirect($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifyErrorMessage={Uri.EscapeDataString(message)}&RedirectUrl={Uri.EscapeDataString(Uri.EscapeDataString($"{GlobalConfiguration.BasePath}{redirectUrl}"))}");

        protected RedirectResult NotifyOfSuccess(string message)
            => Redirect($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifySuccessMessage={Uri.EscapeDataString(message)}");

        protected RedirectResult NotifyOfWarning(string message)
            => Redirect($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifyWarningMessage={Uri.EscapeDataString(message)}");

        protected RedirectResult NotifyOfError(string message)
            => Redirect($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifyErrorMessage={Uri.EscapeDataString(message)}");
    }
}
