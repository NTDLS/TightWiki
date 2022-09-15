using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using AsapWiki.Shared.Classes;
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

            context.IsLoggedIn = true;

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

            return View();
        }
    }
}
