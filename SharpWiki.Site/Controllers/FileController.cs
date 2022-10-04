using SharpWiki.Shared.Models;
using SharpWiki.Shared.Repository;
using System;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SharpWiki.Shared.Library;
using SharpWiki.Shared.Wiki;

namespace SharpWiki.Site.Controllers
{
    [Authorize]
    public class FileController : ControllerHelperBase
    {
        #region Attachments.

        /// <summary>
        /// Allows a user to delete a page attachment from a page.
        /// </summary>
        /// <param name="navigation"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult Delete(string navigation)
        {
            if (context.CanDelete == false)
            {
                return new HttpUnauthorizedResult();
            }

            navigation = WikiUtility.CleanPartialURI(navigation);

            string imageName = Request.QueryString["Image"];
            PageFileRepository.DeletePageFileByPageNavigationAndName(navigation, imageName);

            return RedirectToAction("Upload", "File", new { navigation = navigation });
        }

        /// <summary>
        /// Gets a file from the database and returns it to the client.
        /// </summary>
        /// <param name="navigation"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet]
        public ActionResult Binary(string navigation)
        {
            if (context.CanView == false)
            {
                return new HttpUnauthorizedResult();
            }

            navigation = WikiUtility.CleanPartialURI(navigation);
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
        /// Gets a image from the database, performs optional scaling and returns it to the client.
        /// </summary>
        /// <param name="navigation"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet]
        public ActionResult Image(string navigation)
        {
            if (context.CanView == false)
            {
                return new HttpUnauthorizedResult();
            }

            navigation = WikiUtility.CleanPartialURI(navigation);
            string imageName = Request.QueryString["Image"];
            string scale = Request.QueryString["Scale"] ?? "100";

            var file = PageFileRepository.GetPageFileByPageNavigationAndName(navigation, imageName);

            if (file != null)
            {
                var img = System.Drawing.Image.FromStream(new MemoryStream(file.Data));

                string contentType = file.ContentType;

                ImageFormat format = ImageFormat.Png;
                switch (file.ContentType.ToLower())
                {
                    case "image/png":
                        format = ImageFormat.Png;
                        break;
                    case "image/jpeg":
                        format = ImageFormat.Jpeg;
                        break;
                    case "image/bmp":
                        format = ImageFormat.Bmp;
                        break;
                    case "image/icon":
                        format = ImageFormat.Icon;
                        break;
                    case "image/tiff":
                        format = ImageFormat.Tiff;
                        break;
                    case "image/emf":
                        format = ImageFormat.Emf;
                        break;
                    case "image/exif":
                        format = ImageFormat.Exif;
                        break;
                    case "image/wmf":
                        format = ImageFormat.Wmf;
                        break;
                    default:
                        contentType = "image/png";
                        format = ImageFormat.Png;
                        break;
                }

                int iscale = int.Parse(scale);
                if (iscale != 100)
                {
                    int width = (int)(img.Width * (iscale / 100.0));
                    int height = (int)(img.Height * (iscale / 100.0));
                    using (var bmp = Images.ResizeImage(img, width, height))
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            bmp.Save(ms, format);
                            return File(ms.ToArray(), contentType);
                        }
                    }
                }
                else
                {
                    var bmp = img as System.Drawing.Bitmap;
                    using (MemoryStream ms = new MemoryStream())
                    {
                        bmp.Save(ms, format);
                        return File(ms.ToArray(), contentType);
                    }
                }
            }
            else
            {
                return new HttpNotFoundResult($"[{imageName}] was not found on the page [{navigation}].");
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
            if (context.CanView == false)
            {
                return new HttpUnauthorizedResult();
            }

            navigation = WikiUtility.CleanPartialURI(navigation);
            string imageName = Request.QueryString["Image"];
            string scale = Request.QueryString["Scale"] ?? "100";

            var file = PageFileRepository.GetPageFileByPageNavigationAndName(navigation, imageName);
            if (file != null)
            {
                var img = System.Drawing.Image.FromStream(new MemoryStream(file.Data));

                int iscale = int.Parse(scale);
                if (iscale != 100)
                {
                    int width = (int)(img.Width * (iscale / 100.0));
                    int height = (int)(img.Height * (iscale / 100.0));

                    using (var bmp = Images.ResizeImage(img, width, height))
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
                    var bmp = img as System.Drawing.Bitmap;
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
            if (context.CanCreate == false)
            {
                return new HttpUnauthorizedResult();
            }

            string navigation = WikiUtility.CleanPartialURI(RouteValue("navigation"));
            var page = PageRepository.GetPageInfoByNavigation(navigation);

            HttpPostedFileBase file = Request.Files["BinaryData"];
            int fileSize = file.ContentLength;
            PageFileRepository.UpsertPageFile(new PageFile()
            {
                Data = Utility.ConvertHttpFileToBytes(file),
                CreatedDate = DateTime.UtcNow,
                PageId = page.Id,
                Name = file.FileName,
                Size = fileSize,
                ContentType = MimeMapping.GetMimeMapping(file.FileName)
            });

            var pageFiles = PageFileRepository.GetPageFilesInfoByPageId(page.Id);
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
            if (context.CanCreate == false)
            {
                return new HttpUnauthorizedResult();
            }

            var navigation = WikiUtility.CleanPartialURI(RouteValue("navigation"));
            var page = PageRepository.GetPageByNavigation(navigation);
            if (page != null)
            {
                var pageFiles = PageFileRepository.GetPageFilesInfoByPageId(page.Id);

                return View(new Attachments()
                {
                    Files = pageFiles
                });
            }

            return View(new Attachments()
            {
                Files = new List<PageFile>()
            });
        }

        #endregion
    }
}
