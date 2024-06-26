﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TightWiki.Library;
using TightWiki.Models.ViewModels.Utility;

namespace TightWiki.Site.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class UtilityController : ControllerBase
    {
        public UtilityController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
            : base(signInManager, userManager)
        {
        }

        [AllowAnonymous]
        [HttpGet("NotifyAction")]
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

        [AllowAnonymous]
        [HttpGet("Notify")]
        public ActionResult Notify()
        {
            WikiContext.RequireViewPermission();

            var model = new NotifyViewModel()
            {
                SuccessMessage = GetQueryString("SuccessMessage", string.Empty),
                ErrorMessage = GetQueryString("ErrorMessage", string.Empty),
                RedirectURL = GetQueryString("RedirectURL", string.Empty)
            };

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost("ConfirmAction")]
        public ActionResult ConfirmAction(ConfirmActionViewModel model)
        {
            model.YesRedirectURL = GetFormString("yesRedirectURL").EnsureNotNull();
            model.NoRedirectURL = GetFormString("noRedirectURL").EnsureNotNull();
            model.Message = GetFormString("message").EnsureNotNull();
            model.Style = GetFormString("Style").EnsureNotNull();
            model.Parameter = GetFormString("Parameter");

            return View(model);
        }

        [AllowAnonymous]
        [HttpGet("ConfirmAction")]
        public ActionResult ConfirmAction()
        {
            var model = new ConfirmActionViewModel
            {
                YesRedirectURL = GetQueryString("yesRedirectURL").EnsureNotNull(),
                NoRedirectURL = GetQueryString("noRedirectURL").EnsureNotNull(),
                Message = GetQueryString("message").EnsureNotNull(),
                Style = GetQueryString("Style").EnsureNotNull(),
                Parameter = GetQueryString("Parameter")
            };

            return View(model);
        }
    }
}
