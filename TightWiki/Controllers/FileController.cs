using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using TightWiki.Controllers;
using TightWiki.Shared.Library;
using TightWiki.Shared.Models.Data;
using TightWiki.Shared.Models.View;
using TightWiki.Shared.Repository;
using TightWiki.Shared.Wiki;

namespace TightWiki.Site.Controllers
{
    [Authorize]
    public class FileController : ControllerHelperBase
    {
        /// <summary>
        /// Allows a user to delete a page attachment from a page.
        /// </summary>
        /// <param name="navigation"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult Delete(string pageNavigation, string fileNavigation)
        {
            if (context.CanDelete == false)
            {
                return Unauthorized();
            }

            pageNavigation = WikiUtility.CleanPartialURI(pageNavigation);
            fileNavigation = WikiUtility.CleanPartialURI(fileNavigation);

            PageFileRepository.DeletePageFileByPageNavigationAndFileName(pageNavigation, fileNavigation);

            return RedirectToAction("EditPageAttachment", "File", new { pageNavigation = pageNavigation });
        }

        /// <summary>
        /// Gets a file from the database and returns it to the client.
        /// </summary>
        /// <param name="navigation"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet]
        public ActionResult Binary(string pageNavigation, string fileNavigation, int? pageRevision = null)
        {
            if (context.CanView == false)
            {
                return Unauthorized();
            }

            pageNavigation = WikiUtility.CleanPartialURI(pageNavigation);
            fileNavigation = WikiUtility.CleanPartialURI(fileNavigation);

            var file = PageFileRepository.GetPageFileAttachmentByPageNavigationPageRevisionAndFileNavigation(pageNavigation, fileNavigation, pageRevision);

            if (file != null)
            {
                return File(file.Data.ToArray(), file.ContentType);
            }
            else
            {
                HttpContext.Response.StatusCode = 404;
                return NotFound($"[{fileNavigation}] was not found on the page [{pageNavigation}].");
            }
        }

        /// <summary>
        /// Gets a image from the database, performs optional scaling and returns it to the client.
        /// </summary>
        /// <param name="navigation"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet]
        public ActionResult Image(string pageNavigation, string fileNavigation, int? pageRevision = null)
        {
            if (context.CanView == false)
            {
                return Unauthorized();
            }

            pageNavigation = WikiUtility.CleanPartialURI(pageNavigation);
            fileNavigation = WikiUtility.CleanPartialURI(fileNavigation);

            string scale = Request.Query["Scale"].ToString().IsNullOrEmpty("100");

            var file = PageFileRepository.GetPageFileAttachmentByPageNavigationPageRevisionAndFileNavigation(pageNavigation, fileNavigation, pageRevision);

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
                return NotFound($"[{fileNavigation}] was not found on the page [{pageNavigation}].");
            }
        }

        /// <summary>
        /// Gets a file from the database, converts it to a PNG with optional scaling and returns it to the client.
        /// </summary>
        /// <param name="navigation"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet]
        public ActionResult Png(string pageNavigation, string fileNavigation, int? pageRevision = null)
        {
            if (context.CanView == false)
            {
                return Unauthorized();
            }

            pageNavigation = WikiUtility.CleanPartialURI(pageNavigation);
            fileNavigation = WikiUtility.CleanPartialURI(fileNavigation);

            string scale = Request.Query["Scale"].ToString().IsNullOrEmpty("100");

            var file = PageFileRepository.GetPageFileAttachmentByPageNavigationPageRevisionAndFileNavigation(pageNavigation, fileNavigation, pageRevision);
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
                return NotFound($"[{fileNavigation}] was not found on the page [{pageNavigation}].");
            }
        }

        /// <summary>
        /// Allows the use to upload a file/attachemnt to a page.
        /// </summary>
        /// <param name="postData"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public ActionResult EditPageAttachment(string pageNavigation, object postData)
        {
            if (context.CanCreate == false)
            {
                return Unauthorized();
            }

            pageNavigation = WikiUtility.CleanPartialURI(pageNavigation);
            var page = PageRepository.GetPageInfoByNavigation(pageNavigation);

            var file = Request.Form.Files["BinaryData"];
            if (file != null)
            {
                var fileSize = file.Length;
                if (fileSize > 0)
                {
                    PageFileRepository.UpsertPageFile(new PageFileAttachment()
                    {
                        Data = Utility.ConvertHttpFileToBytes(file),
                        CreatedDate = DateTime.UtcNow,
                        PageId = page.Id,
                        Name = file.FileName,
                        Size = fileSize,
                        ContentType = Utility.GetMimeType(file.FileName)
                    });
                }
            }

            var pageFiles = PageFileRepository.GetPageFilesInfoByPageIdAndPageRevision(page.Id);
            return View(new FileAttachmentModel()
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
        public ActionResult EditPageAttachment(string pageNavigation)
        {
            if (context.CanCreate == false)
            {
                return Unauthorized();
            }

            pageNavigation = WikiUtility.CleanPartialURI(pageNavigation);
            var page = PageRepository.GetPageRevisionByNavigation(pageNavigation);
            if (page != null)
            {
                var pageFiles = PageFileRepository.GetPageFilesInfoByPageIdAndPageRevision(page.Id);

                return View(new FileAttachmentModel()
                {
                    Files = pageFiles
                });
            }

            return View(new FileAttachmentModel()
            {
                Files = new List<PageFileAttachment>()
            });
        }
    }
}
