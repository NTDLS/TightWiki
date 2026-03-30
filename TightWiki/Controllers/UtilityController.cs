using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NTDLS.Helpers;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;
using TightWiki.ViewModels.Utility;

namespace TightWiki.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class UtilityController(ILogger<ITwEngine> logger, SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager, ITwSharedLocalizationText localizer, TwConfiguration wikiConfiguration)
        : WikiControllerBase<UtilityController>(logger, signInManager, userManager, localizer, wikiConfiguration)
    {
        [AllowAnonymous]
        [HttpGet("Notify")]
        public ActionResult Notify()
        {
            try
            {
                var model = new NotifyViewModel()
                {
                    NotifySuccessMessage = GetQueryValue("NotifySuccessMessage", string.Empty),
                    NotifyErrorMessage = GetQueryValue("NotifyErrorMessage", string.Empty),
                    NotifyWarningMessage = GetQueryValue("NotifyWarningMessage", string.Empty),
                    RedirectURL = GetQueryValue("RedirectURL", string.Empty),
                    RedirectTimeout = GetQueryValue("RedirectTimeout", 0)
                };

                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in Notify action");
                throw;
            }
        }

        [AllowAnonymous]
        [HttpPost("ConfirmAction")]
        public ActionResult ConfirmAction(ConfirmActionViewModel model)
        {
            try
            {
                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in ConfirmAction POST action");
                throw;
            }
        }

        [AllowAnonymous]
        [HttpGet("ConfirmAction")]
        public ActionResult ConfirmAction()
        {
            try
            {
                var model = new ConfirmActionViewModel
                {
                    ControllerURL = GetQueryValue<string>("controllerURL").EnsureNotNull(),
                    YesRedirectURL = GetQueryValue<string>("yesRedirectURL").EnsureNotNull(),
                    NoRedirectURL = GetQueryValue<string>("noRedirectURL").EnsureNotNull(),
                    Message = GetQueryValue<string>("message").EnsureNotNull(),
                    Style = GetQueryValue<string>("Style").EnsureNotNull()
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