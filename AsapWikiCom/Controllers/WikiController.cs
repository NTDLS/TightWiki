using AsapWiki.Shared.Classes;
using AsapWiki.Shared.Models;
using AsapWiki.Shared.Repository;
using AsapWiki.Shared.Wiki;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace AsapWikiCom.Controllers
{
    [Authorize]
    public class WikiController : ControllerHelperBase
    {
        #region View Page.

        [AllowAnonymous]
        public ActionResult Content()
        {
            Configure();
            string navigation = Utility.CleanPartialURI(RouteValue("navigation"));

            var page = PageRepository.GetPageByNavigation(navigation);
            if (page != null)
            {
                var wiki = new Wikifier(context, page);

                ViewBag.Title = page.Name;
                ViewBag.Body = wiki.ProcessedBody;
            }

            return View();
        }

        #endregion

        #region Edit.

        [Authorize]
        [HttpGet]
        public ActionResult Edit()
        {
            Configure();

            string navigation = Utility.CleanPartialURI(RouteValue("navigation"));

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
                    Navigation = Utility.CleanPartialURI(page.Navigation),
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
                    Navigation = Utility.CleanPartialURI(navigation)
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
                Page page;

                if (editPage.Id == 0)
                {
                    page = new Page()
                    {
                        CreatedDate = DateTime.UtcNow,
                        CreatedByUserId = context.User.Id,
                        ModifiedDate = DateTime.UtcNow,
                        ModifiedByUserId = context.User.Id,
                        Body = editPage.Body ?? "",
                        Name = editPage.Name,
                        Navigation = Utility.CleanPartialURI(editPage.Name),
                        Description = editPage.Description ?? ""
                    };

                    page.Id = PageRepository.SavePage(page);

                    var wikifier = new Wikifier(context, page);
                    PageTagRepository.UpdatePageTags(page.Id, wikifier.Tags);
                    ProcessingInstructionRepository.UpdatePageProcessingInstructions(page.Id, wikifier.ProcessingInstructions);
                    var pageTokens = wikifier.ParsePageTokens().Select(o => o.ToPageToken(page.Id)).ToList();
                    PageRepository.SavePageTokens(pageTokens);

                    return RedirectToAction("Edit", "Wiki", new { navigation = page.Navigation });
                }
                else
                {
                    page = PageRepository.GetPageById(editPage.Id);
                    page.ModifiedDate = DateTime.UtcNow;
                    page.ModifiedByUserId = context.User.Id;
                    page.Body = editPage.Body ?? "";
                    page.Name = editPage.Name;
                    page.Navigation = Utility.CleanPartialURI(editPage.Name);
                    page.Description = editPage.Description ?? "";

                    PageRepository.SavePage(page);

                    var wikifier = new Wikifier(context, page);
                    PageTagRepository.UpdatePageTags(page.Id, wikifier.Tags);
                    ProcessingInstructionRepository.UpdatePageProcessingInstructions(page.Id, wikifier.ProcessingInstructions);
                    var pageTokens = wikifier.ParsePageTokens().Select(o => o.ToPageToken(page.Id)).ToList();
                    PageRepository.SavePageTokens(pageTokens);

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

        #endregion

        #region Attachments.

        /// <summary>
        /// Allows a user to delete a page attachment from a page.
        /// </summary>
        /// <param name="navigation"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet]
        public ActionResult DeleteAttachment(string navigation)
        {
            navigation = Utility.CleanPartialURI(navigation);

            string imageName = Request.QueryString["Image"];
            var page = PageRepository.GetPageByNavigation(navigation);

            PageFileRepository.DeletePageFileByPageNavigationAndName(navigation, imageName);

            return RedirectToAction("Upload", "Wiki", new { navigation = page.Id });
        }

        /// <summary>
        /// Gets a file from the database and returns it to the client.
        /// </summary>
        /// <param name="navigation"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet]
        public ActionResult Attachment(string navigation)
        {
            navigation = Utility.CleanPartialURI(navigation);
            string attachmentName = Request.QueryString["file"];

            var file = PageFileRepository.GetPageFileByPageNavigationAndName(navigation, attachmentName);

            if (file != null)
            {
                return File(file.Data.ToArray(), file.ContentType);
            }
            else
            {
                return new HttpNotFoundResult($"[{attachmentName}] was not found on the page [{navigation}].");
            }
        }

        /// <summary>
        /// Gets a file from the database, converts it to a PNG with optional scaling and returns it to the client.
        /// </summary>
        /// <param name="navigation"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet]
        public ActionResult Png(string navigation)
        {
            navigation = Utility.CleanPartialURI(navigation);
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
                return new HttpNotFoundResult($"[{imageName}] was not found on the page [{navigation}].");
            }
        }

        /// <summary>
        /// Allows the use to upload a file/attachemnt to a page.
        /// </summary>
        /// <param name="postData"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public ActionResult Upload(object postData)
        {
            Configure();

            string navigation = Utility.CleanPartialURI(RouteValue("navigation"));
            int pageId = int.Parse(navigation);

            HttpPostedFileBase file = Request.Files["ImageData"];
            PageFileRepository.UpsertPageFile(new PageFile()
            {
                Data = ConvertToBytes(file),
                CreatedDate = DateTime.UtcNow,
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

        /// <summary>
        /// Populate the upload page. Shows the attachments.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet]
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

        #endregion
    }
}
