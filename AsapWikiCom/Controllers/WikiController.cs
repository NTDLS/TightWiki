using AsapWiki.Shared.Classes;
using AsapWiki.Shared.Models;
using AsapWiki.Shared.Repository;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Web;
using System.Web.Mvc;

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

        [AllowAnonymous]
        [HttpGet]
        public ActionResult DeleteFile(string navigation)
        {
            string imageName = Request.QueryString["Image"];
            var page = PageRepository.GetPageByNavigation(navigation);

            PageFileRepository.DeletePageFileByPageNavigationAndName(navigation, imageName);

            return RedirectToAction("Upload", "Wiki", new { navigation = page.Id });
        }

        /// <summary>
        /// Gets a file from the database and converts it to a PNG with optional scaling.
        /// </summary>
        /// <param name="navigation"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet]
        public ActionResult Png(string navigation)
        {
            string imageName = Request.QueryString["Image"];
            string scale = Request.QueryString["Scale"] ?? "100";

            var file = PageFileRepository.GetPageFileByPageNavigationAndName(navigation, imageName);

            if (file != null)
            {
                var img = Image.FromStream(new MemoryStream(file.Data));

                int iscale = int.Parse(scale);
                if (iscale != 100)
                {
                    int width = (int)(img.Width * (iscale / 100.0));
                    int height = (int)(img.Height * (iscale / 100.0));

                    using (Bitmap bmp = Images.ResizeImage(img, width, height) as Bitmap)
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            bmp.Save(ms, ImageFormat.Png);
                            return File(ms.ToArray(), "image/png");
                        }
                    }
                }
                else
                {
                    var bmp = img as Bitmap;
                    using (MemoryStream ms = new MemoryStream())
                    {
                        bmp.Save(ms, ImageFormat.Png);
                        return File(ms.ToArray(), "image/png");
                    }
                }
            }
            else
            {
                return null;
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
            PageFileRepository.UpsertPageFile(new PageFile()
            {
                Data = ConvertToBytes(file),
                CreatedDate = DateTime.Now,
                PageId = pageId,
                Name = file.FileName,
                Size = file.ContentLength,
                ContentType = MimeMapping.GetMimeMapping(file.FileName)
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
            int pageId = int.Parse(RouteValue("navigation"));

            var page = PageRepository.GetPageById(pageId);

            ViewBag.Navigation = page.Navigation;

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
                Page page;

                if (editPage.Id == 0)
                {
                    page = new Page()
                    {
                        CreatedDate = DateTime.Now,
                        CreatedByUserId = context.User.Id,
                        ModifiedDate = DateTime.Now,
                        ModifiedByUserId = context.User.Id,
                        Body = editPage.Body ?? "",
                        Name = editPage.Name,
                        Navigation = HTML.CleanPartialURI(editPage.Name),
                        Description = editPage.Description ?? ""
                    };

                    var tags = page.HashTags();
                    page.Id = PageRepository.InsertPage(page);
                    PageTagRepository.UpdatePageTags(page.Id, tags);

                    return RedirectToAction("Edit", "Wiki", new { navigation = page.Navigation });
                }
                else
                {
                    page = PageRepository.GetPageById(editPage.Id);
                    page.ModifiedDate = DateTime.Now;
                    page.ModifiedByUserId = context.User.Id;
                    page.Body = editPage.Body ?? "";
                    page.Name = editPage.Name;
                    page.Navigation = HTML.CleanPartialURI(editPage.Name);
                    page.Description = editPage.Description ?? "";

                    var tags = page.HashTags();
                    PageRepository.UpdatePageById(page);
                    PageTagRepository.UpdatePageTags(editPage.Id, tags);

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
