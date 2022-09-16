using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;
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

                string newPageTemplate = ConfigurationEntryRepository.GetConfigurationEntryValuesByGroupNameAndEntryName("Basic", "New Page Template");

                return View(new EditPage()
                {
                    Body = newPageTemplate,
                    Name = pageName,
                    Navigation = navigation
                });
            }
        }



        [Authorize]
        [HttpPost]
        public ActionResult Upload(object postData)
        {
            Configure();

            string navigation = RouteValue("navigation");
            int pageId = int.Parse(navigation);

            HttpPostedFileBase file = Request.Files["ImageData"];
            PageFileRepository.InsertPageFile(new PageFile()
            {
                Data = ConvertToBytes(file),
                CreatedDate = DateTime.Now,
                PageId = pageId,
                Name = file.FileName,
                Size = file.ContentLength
            });

            var pageFiles = PageFileRepository.GetPageFilesInfoByPageId(pageId);
            return View(new Attachments()
            {
                Files = pageFiles
            });
        }

        [Authorize]
        public ActionResult Upload()
        {
            Configure();
            string navigation = RouteValue("navigation");
            int pageId = int.Parse(navigation);

            var pageFiles = PageFileRepository.GetPageFilesInfoByPageId(pageId);
            return View(new Attachments()
            {
                Files = pageFiles
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult Edit(EditPage editPage)
        {
            Configure();

            if (ModelState.IsValid)
            {
                Page page = null;

                if (editPage.Id == 0)
                {
                    page = new Page()
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
                    page.Id = PageRepository.InsertPage(page);
                    PageTagRepository.UpdatePageTags(page.Id, tags);
                }
                else
                {
                    page = PageRepository.GetPageById(editPage.Id);
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

                if (page != null)
                {
                    return View(new EditPage()
                    {
                        Id = page.Id,
                        Body = page.Body,
                        Name = page.Name,
                        Navigation = page.Navigation,
                        Description = page.Description
                    });
                }
            }
            return View();
        }
    }
}
