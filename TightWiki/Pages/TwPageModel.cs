using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;

namespace TightWiki.Pages
{
    public class TwPageModel(ILogger<ITwEngine> logger, SignInManager<IdentityUser> signInManager,
            ITwSharedLocalizationText localizer, TwConfiguration wikiConfiguration, ITwDatabaseManager databaseManager)
        : PageModel
    {
        public ITwSharedLocalizationText Localizer { get; private set; } = localizer;

        public TwSessionState SessionState { get; private set; } = new();
        public SignInManager<IdentityUser> SignInManager { get; private set; } = signInManager;
        public TwConfiguration WikiConfiguration { get; private set; } = wikiConfiguration;

        public string SuccessMessage { get; set; } = string.Empty;
        public string WarningMessage { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;

        public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
        {
            var sessionState = await SessionState.Hydrate(logger, SignInManager, this, WikiConfiguration, databaseManager);
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
            => Redirect($"{WikiConfiguration.BasePath}/Utility/Notify?NotifySuccessMessage={Uri.EscapeDataString(message)}&RedirectUrl={Uri.EscapeDataString($"{WikiConfiguration.BasePath}{redirectUrl}")}&RedirectTimeout=5");

        protected RedirectResult NotifyOfWarning(string message, string redirectUrl)
            => Redirect($"{WikiConfiguration.BasePath}/Utility/Notify?NotifyWarningMessage={Uri.EscapeDataString(message)}&RedirectUrl={Uri.EscapeDataString(Uri.EscapeDataString($"{WikiConfiguration.BasePath}{redirectUrl}"))}");

        protected RedirectResult NotifyOfError(string message, string redirectUrl)
            => Redirect($"{WikiConfiguration.BasePath}/Utility/Notify?NotifyErrorMessage={Uri.EscapeDataString(message)}&RedirectUrl={Uri.EscapeDataString(Uri.EscapeDataString($"{WikiConfiguration.BasePath}{redirectUrl}"))}");

        protected RedirectResult NotifyOfSuccess(string message)
            => Redirect($"{WikiConfiguration.BasePath}/Utility/Notify?NotifySuccessMessage={Uri.EscapeDataString(message)}");

        protected RedirectResult NotifyOfWarning(string message)
            => Redirect($"{WikiConfiguration.BasePath}/Utility/Notify?NotifyWarningMessage={Uri.EscapeDataString(message)}");

        protected RedirectResult NotifyOfError(string message)
            => Redirect($"{WikiConfiguration.BasePath}/Utility/Notify?NotifyErrorMessage={Uri.EscapeDataString(message)}");
    }
}
