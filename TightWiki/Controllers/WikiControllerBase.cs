using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Localization;
using NTDLS.Helpers;
using Org.BouncyCastle.Asn1.Ocsp;
using TightWiki.Models;

namespace TightWiki.Controllers
{
    public class WikiControllerBase<T>(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, IStringLocalizer<T> localizer)
        : Controller
    {
        public SessionState SessionState { get; private set; } = new();

        public readonly SignInManager<IdentityUser> SignInManager = signInManager;
        public readonly UserManager<IdentityUser> UserManager = userManager;

        [NonAction]
        public override void OnActionExecuting(ActionExecutingContext filterContext)
            => ViewData["SessionState"] = SessionState.Hydrate(SignInManager, this);

        [NonAction]
        public override RedirectResult Redirect(string? url)
            => base.Redirect(url.EnsureNotNull());

        [NonAction]
        protected V? GetQueryValue<V>(string key)
        {
            if (Request.Query.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
            {
                return Converters.ConvertToNullable<V>(value);
            }
            return default;
        }
        [NonAction]
        protected V GetQueryValue<V>(string key, V defaultValue)
        {
            if (Request.Query.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
            {
                return Converters.ConvertToNullable<V>(value) ?? defaultValue;
            }
            return defaultValue;
        }

        [NonAction]
        protected string? GetFormValue(string key)
            => Request.Form[key];

        [NonAction]
        protected string GetFormValue(string key, string defaultValue)
            => (string?)Request.Form[key] ?? defaultValue;

        [NonAction]
        protected int GetFormValue(string key, int defaultValue)
            => int.Parse(GetFormValue(key, defaultValue.ToString()));

        [NonAction]
        protected string Localize(string key)
            => localizer[key].Value;

        [NonAction]
        protected string Localize(string key, params object[] objs)
            => string.Format(localizer[key].Value, objs);

        /// <summary>
        /// Displays the successMessage unless the errorMessage is present.
        /// </summary>
        protected RedirectResult NotifyOf(string successMessage, string errorMessage, string redirectUrl)
            => Redirect($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifySuccessMessage={Uri.EscapeDataString(successMessage)}&NotifyErrorMessage={Uri.EscapeDataString(errorMessage)}&RedirectUrl={Uri.EscapeDataString($"{GlobalConfiguration.BasePath}{redirectUrl}")}&RedirectTimeout=5");

        protected RedirectResult NotifyOfSuccess(string message, string redirectUrl)
            => Redirect($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifySuccessMessage={Uri.EscapeDataString(message)}&RedirectUrl={Uri.EscapeDataString($"{GlobalConfiguration.BasePath}{redirectUrl}")}&RedirectTimeout=5");

        protected RedirectResult NotifyOfWarning(string message, string redirectUrl)
            => Redirect($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifyWarningMessage={Uri.EscapeDataString(message)}&RedirectUrl={Uri.EscapeDataString($"{GlobalConfiguration.BasePath}{redirectUrl}")}");

        protected RedirectResult NotifyOfError(string message, string redirectUrl)
            => Redirect($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifyErrorMessage={Uri.EscapeDataString(message)}&RedirectUrl={Uri.EscapeDataString($"{GlobalConfiguration.BasePath}{redirectUrl}")}");

        protected RedirectResult NotifyOfSuccess(string message)
            => Redirect($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifySuccessMessage={Uri.EscapeDataString(message)}");

        protected RedirectResult NotifyOfWarning(string message)
            => Redirect($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifyWarningMessage={Uri.EscapeDataString(message)}");

        protected RedirectResult NotifyOfError(string message)
            => Redirect($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifyErrorMessage={Uri.EscapeDataString(message)}");
    }
}
