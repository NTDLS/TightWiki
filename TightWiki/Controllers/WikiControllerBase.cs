using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NTDLS.Helpers;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.Library;
using TightWiki.Models;

namespace TightWiki.Controllers
{
    public class WikiControllerBase<T>(ILogger<ITightEngine> logger, SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager, ISharedLocalizationText localizer, TightWikiConfiguration wikiConfiguration)
        : Controller
    {
        public TightWikiConfiguration WikiConfiguration { get; private set; } = wikiConfiguration;
        public ILogger<ITightEngine> Logger { get; private set; } = logger;
        public ISharedLocalizationText Localizer { get; private set; } = localizer;
        public SessionState SessionState { get; private set; } = new();

        public readonly SignInManager<IdentityUser> SignInManager = signInManager;
        public readonly UserManager<IdentityUser> UserManager = userManager;

        [NonAction]
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var sessionState = await SessionState.Hydrate(Logger, SignInManager, this, WikiConfiguration);
            ViewData["SessionState"] = sessionState;
            await next();
        }

        [NonAction]
        public override RedirectResult Redirect(string? url)
        {
            try
            {
                return base.Redirect(url.EnsureNotNull());
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in Redirect to {Url}", url);
                throw;
            }
        }

        [NonAction]
        protected V? GetQueryValue<V>(string key)
        {
            try
            {
                if (Request.Query.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
                {
                    return Converters.ConvertToNullable<V>(value);
                }
                return default;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error getting query value for key {Key}", key);
                throw;
            }
        }

        [NonAction]
        protected V GetQueryValue<V>(string key, V defaultValue)
        {
            try
            {
                if (Request.Query.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
                {
                    return Converters.ConvertToNullable<V>(value) ?? defaultValue;
                }
                return defaultValue;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error getting query value for key {Key} with default value {DefaultValue}", key, defaultValue);
                throw;
            }
        }

        [NonAction]
        protected string? GetFormValue(string key)
        {
            try
            {
                return Request.Form[key];
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error getting form value for key {Key}", key);
                throw;
            }
        }

        [NonAction]
        protected string GetFormValue(string key, string defaultValue)
        {
            try
            {
                return (string?)Request.Form[key] ?? defaultValue;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error getting form value for key {Key} with default value {DefaultValue}", key, defaultValue);
                throw;
            }
        }

        [NonAction]
        protected int GetFormValue(string key, int defaultValue)
        {
            try
            {
                return int.Parse(GetFormValue(key, defaultValue.ToString()));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error getting form value for key {Key} with default int value {DefaultValue}", key, defaultValue);
                throw;
            }
        }

        [NonAction]
        protected string Localize(string key)
        {
            try
            {
                return Localizer[key].Value;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error localizing key {Key}", key);
                throw;
            }
        }

        [NonAction]
        protected string Localize(string key, params object?[] objs)
        {
            try
            {
                return string.Format(Localizer[key].Value, objs);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error localizing key {Key} with parameters {Parameters}", key, objs);
                throw;
            }
        }

        /// <summary>
        /// Displays the successMessage unless the errorMessage is present.
        /// </summary>
        protected RedirectResult NotifyOf(string successMessage, string errorMessage, string redirectUrl)
        {
            try
            {
                return Redirect($"{WikiConfiguration.BasePath}/Utility/Notify?NotifySuccessMessage={Uri.EscapeDataString(successMessage)}&NotifyErrorMessage={Uri.EscapeDataString(errorMessage)}&RedirectUrl={Uri.EscapeDataString($"{WikiConfiguration.BasePath}{redirectUrl}")}&RedirectTimeout=5");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error notifying of success message {SuccessMessage} and error message {ErrorMessage} with redirect URL {RedirectUrl}", successMessage, errorMessage, redirectUrl);
                throw;
            }
        }

        protected RedirectResult NotifyOfSuccess(string message, string redirectUrl)
        {
            try
            {
                return Redirect($"{WikiConfiguration.BasePath}/Utility/Notify?NotifySuccessMessage={Uri.EscapeDataString(message)}&RedirectUrl={Uri.EscapeDataString($"{WikiConfiguration.BasePath}{redirectUrl}")}&RedirectTimeout=5");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error notifying of success message {Message} with redirect URL {RedirectUrl}", message, redirectUrl);
                throw;
            }
        }

        protected RedirectResult NotifyOfWarning(string message, string redirectUrl)
        {
            try
            {
                return Redirect($"{WikiConfiguration.BasePath}/Utility/Notify?NotifyWarningMessage={Uri.EscapeDataString(message)}&RedirectUrl={Uri.EscapeDataString($"{WikiConfiguration.BasePath}{redirectUrl}")}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error notifying of warning message {Message} with redirect URL {RedirectUrl}", message, redirectUrl);
                throw;
            }
        }

        protected RedirectResult NotifyOfError(string message, string redirectUrl)
        {
            try
            {
                return Redirect($"{WikiConfiguration.BasePath}/Utility/Notify?NotifyErrorMessage={Uri.EscapeDataString(message)}&RedirectUrl={Uri.EscapeDataString($"{WikiConfiguration.BasePath}{redirectUrl}")}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error notifying of error message {Message} with redirect URL {RedirectUrl}", message, redirectUrl);
                throw;
            }
        }

        protected RedirectResult NotifyOfSuccess(string message)
        {
            try
            {
                return Redirect($"{WikiConfiguration.BasePath}/Utility/Notify?NotifySuccessMessage={Uri.EscapeDataString(message)}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error notifying of success message {Message}", message);
                throw;
            }
        }

        protected RedirectResult NotifyOfWarning(string message)
        {
            try
            {
                return Redirect($"{WikiConfiguration.BasePath}/Utility/Notify?NotifyWarningMessage={Uri.EscapeDataString(message)}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error notifying of warning message {Message}", message);
                throw;
            }
        }

        protected RedirectResult NotifyOfError(string message)
        {
            try
            {
                return Redirect($"{WikiConfiguration.BasePath}/Utility/Notify?NotifyErrorMessage={Uri.EscapeDataString(message)}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error notifying of error message {Message}", message);
                throw;
            }
        }
    }
}
