using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;
using TightWiki.ViewModels.Utility;

namespace TightWiki.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class UtilityController(
            ILogger<ITwEngine> logger,
            ITwSharedLocalizationText localizer,
            SignInManager<IdentityUser> signInManager,
            TwConfiguration wikiConfiguration,
            UserManager<IdentityUser> userManager,
            ITwDatabaseManager databaseManager
        )
        : TwController<UtilityController>(logger, signInManager, userManager, localizer, wikiConfiguration, databaseManager)
    {
        [AllowAnonymous]
        [HttpGet("Notify")]
        public ActionResult Notify(string? t)
        {
            try
            {
                var model = new NotifyViewModel();

                //The content of this page only ever comes from a token signed by the wiki, never from the query string.
                if (TwSignedUrl.TryUnprotect<TwNotifyPayload>(t, out var payload) && payload != null)
                {
                    model.NotifySuccessMessage = payload.Success ?? string.Empty;
                    model.NotifyWarningMessage = payload.Warning ?? string.Empty;
                    model.NotifyErrorMessage = payload.Error ?? string.Empty;
                    model.RedirectURL = Url.IsLocalUrl(payload.RedirectUrl) ? payload.RedirectUrl : string.Empty;
                    model.RedirectTimeout = payload.RedirectTimeout;
                }
                else
                {
                    model.NotifyWarningMessage = Localize("This notification has expired or is not valid.");
                    model.RedirectURL = $"{WikiConfiguration.BasePath}/";
                }

                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in Notify action");
                throw;
            }
        }

        [AllowAnonymous]
        [HttpGet("ConfirmAction")]
        public ActionResult ConfirmAction(string? t)
        {
            try
            {
                //The message and the action that "yes" posts to only ever come from a token signed by the wiki.
                if (!TwSignedUrl.TryUnprotect<TwConfirmActionPayload>(t, out var payload) || payload == null)
                {
                    return NotifyOfWarning(Localize("This confirmation link has expired or is not valid."), "/");
                }

                var model = new ConfirmActionViewModel
                {
                    ControllerURL = payload.ControllerURL,
                    YesRedirectURL = payload.YesRedirectURL,
                    NoRedirectURL = payload.NoRedirectURL,
                    Message = payload.Message,
                    Style = payload.Style
                };

                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in ConfirmAction GET action");
                throw;
            }
        }
    }
}