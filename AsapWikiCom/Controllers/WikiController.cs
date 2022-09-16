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
                //Editing an existing page.
                ViewBag.Title = page.Name;

                return View(new EditPage()
                {
                    Id = page.Id,
                    Body = page.Body,
                    Name = page.Name,
                    Navigation = page.Navigation,
                    Description = page.Description
                });
            }
            else
            {
                var pageName = Request.QueryString["Name"] ?? navigation;

                return View(new EditPage()
                {
                    Body = "#Draft\r\n\r\n",
                    Name = pageName,
                    Navigation = navigation
                });
            }
        }

        [Authorize]
        [HttpPost]
        public ActionResult Edit(EditPage editPage)
        {
            Configure();

            if (ModelState.IsValid)
            {
                if (editPage.Id == 0)
                {
                    var page = new Page()
                    {
                        CreatedDate = DateTime.Now,
                        CreatedByUserId = context.User.Id,
                        ModifiedDate = DateTime.Now,
                        ModifiedByUserId = context.User.Id,
                        Body = editPage.Body,
                        Name = editPage.Name,
                        Navigation = HTML.CleanPartialURI(editPage.Name),
                        Description = editPage.Description
                    };

                    var tags = page.HashTags();
                    int pageId = PageRepository.InsertPage(page);
                    PageTagRepository.UpdatePageTags(pageId, tags);
                }
                else
                {
                    var page = PageRepository.GetPageById(editPage.Id);

                    page.ModifiedDate = DateTime.Now;
                    page.ModifiedByUserId = context.User.Id;
                    page.Body = editPage.Body;
                    page.Name = editPage.Name;
                    page.Navigation = HTML.CleanPartialURI(editPage.Name);
                    page.Description = editPage.Description;

                    var tags = page.HashTags();
                    PageRepository.UpdatePageById(page);
                    PageTagRepository.UpdatePageTags(editPage.Id, tags);
                }

                //ModelState.AddModelError("", "wtf did you do man?!");
            }
            return View();
        }
    }
}
