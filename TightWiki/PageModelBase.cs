using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.Library;
using TightWiki.Models;

namespace TightWiki
{
    public class PageModelBase
        : PageModel
    {
        public ISharedLocalizationText Localizer { get; private set; }

        public SessionState SessionState { get; private set; } = new();
        public SignInManager<IdentityUser> SignInManager { get; private set; }

        public string SuccessMessage { get; set; } = string.Empty;
        public string WarningMessage { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        public ILogger<ITightEngine> Logger { get; private set; }

        public PageModelBase(ILogger<ITightEngine> logger, SignInManager<IdentityUser> signInManager, ISharedLocalizationText localizer)
        {
            Localizer = localizer;
            Logger = logger;
            SignInManager = signInManager;
        }

        public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
        {
            var sessionState = await SessionState.Hydrate(Logger, SignInManager, this);
            ViewData["SessionState"] = sessionState;
            await next();
        }

        [NonAction]
        protected string Localize(string key)
            => Localizer[key].Value;

        [NonAction]
        protected string Localize(string key, params object?[] objs)
            => string.Format(Localizer[key].Value, objs);

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
