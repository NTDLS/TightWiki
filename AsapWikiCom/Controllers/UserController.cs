using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using AsapWiki.Shared.Classes;
using AsapWiki.Shared.Repository;

namespace AsapWikiCom.Controllers
{
    [Authorize]
    public class UserController : ControllerHelperBase
    {
        [AllowAnonymous]
        public ActionResult Login()
        {
            Configure();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public ActionResult Login(AsapWiki.Shared.Models.FormLogin user)
        {
            if (ModelState.IsValid)
            {
                //FormsAuthentication.SetAuthCookie("DaUser", false);
                //return RedirectToAction("Show", "Wiki", "Home");
            }
            ModelState.AddModelError("", "invalid Username or Password");
            return View();
        }

        [AllowAnonymous]
        public ActionResult Forgot()
        {
            Configure();
            return View();
        }

        [AllowAnonymous]
        public ActionResult Reset()
        {
            Configure();
            return View();
        }

        [AllowAnonymous]
        public ActionResult Signup()
        {
            Configure();
            return View();
        }

        [Authorize]
        public ActionResult UserProfile()
        {
            Configure();
            return View();
        }

    }
}
