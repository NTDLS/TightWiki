using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TightWiki.Models.ViewModels.Utility;

namespace TightWiki.Site.Controllers
{
    [Authorize]
    public class UtilityController : ControllerBase
    {
        public UtilityController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
            : base(signInManager, userManager)
        {
        }

        [AllowAnonymous]
        public ActionResult NotifyAction()
        {
            WikiContext.RequireViewPermission();

            var model = new NotifyActionViewModel()
            {
                SuccessMessage = GetQueryString("SuccessMessage", string.Empty),
                ErrorMessage = GetQueryString("ErrorMessage", string.Empty),
                RedirectURL = GetQueryString("RedirectURL", string.Empty)
            };

            return View(model);
        }
    }
}
