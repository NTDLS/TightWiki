using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web.Mvc;
using System.Web.Security;
using AsapWiki.Shared.Classes;
using AsapWiki.Shared.Models;
using AsapWiki.Shared.Repository;

namespace AsapWikiCom.Controllers
{
    [Authorize]
    public class WikiController : ControllerHelperBase
    {
        [AllowAnonymous]
        public ActionResult Show()
        {
            Configure();

            string navigation = RouteValue("navigation");

            var page = PageRepository.GetPageByNavigation(navigation);
            if (page != null)
            {
                ViewBag.Title = page.Name;
                var wikifier = new AsapWiki.Shared.Wiki.Wikifier(context);
                ViewBag.Body = wikifier.Transform(page);
            }

            return View();
        }

        [Authorize]
        public ActionResult Edit()
        {
            Configure();

            string navigation = RouteValue("navigation");

            var page = PageRepository.GetPageByNavigation(navigation);
            if (page != null)
            {
                ViewBag.Title = page.Name;

                return View(new Page()
                {
                     Body = page.Body
                });
            }

            return View();
        }

        [Authorize]
        [HttpPost]
        public ActionResult Edit(AsapWiki.Shared.Models.Page editPage)
        {
            if (ModelState.IsValid)
            {
                ModelState.AddModelError("", "wtf did you do man?!");
            }
            return View();
        }
    }
}
