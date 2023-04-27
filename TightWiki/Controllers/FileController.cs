using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using TightWiki.Controllers;
using TightWiki.Shared.Library;
using TightWiki.Shared.Models.Data;
using TightWiki.Shared.Models.View;
using TightWiki.Shared.Repository;
using TightWiki.Shared.Wiki;
using static TightWiki.Shared.Library.Images;

namespace TightWiki.Site.Controllers
{
    [Authorize]
    public class FileController : ControllerHelperBase
    {
        /// <summary>
        /// Uploads a file by drag drop.
        /// </summary>
        /// <param name="pageNavigation"></param>
        /// <param name="postedFiles"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public IActionResult UploadDragDrop(string pageNavigation, List<IFormFile> postedFiles)
        {
            if (context.CanCreate == false)
            {
                return Unauthorized();
            }

            pageNavigation = WikiUtility.CleanPartialURI(pageNavigation);
            var page = PageRepository.GetPageInfoByNavigation(pageNavigation);

            foreach (IFormFile file in postedFiles)
            {
                if (file != null)
                {
                    var fileSize = file.Length;
                    if (fileSize > 0)
                    {
                        var fileName = HttpUtility.UrlDecode(file.FileName);

                        PageFileRepository.UpsertPageFile(new PageFileAttachment()
                        {
                            Data = Utility.ConvertHttpFileToBytes(file),
                            CreatedDate = DateTime.UtcNow,
                            PageId = page.Id,
                            Name = fileName,
                            Size = fileSize,
                            ContentType = Utility.GetMimeType(fileName)
                        });
                        return Content("Success");
                    }
                }
            }

            return Content("Failure");
        }

        /// <summary>
        /// Uploads a file by manually selecting it for upload.
        /// </summary>
        /// <param name="postData"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public IActionResult UploadManual(string pageNavigation, IFormFile formFile)
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
                    var fileName = HttpUtility.UrlDecode(file.FileName);

                    PageFileRepository.UpsertPageFile(new PageFileAttachment()
                    {
                        Data = Utility.ConvertHttpFileToBytes(file),
                        CreatedDate = DateTime.UtcNow,
                        PageId = page.Id,
                        Name = fileName,
                        Size = fileSize,
                        ContentType = Utility.GetMimeType(fileName)
                    });

                    return Content("Success");
                }
            }

            return Content("Failure");
        }

        /// <summary>
        /// Allows a user to delete a page attachment from a page.
        /// </summary>
        /// <param name="navigation"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Delete(string pageNavigation, string fileNavigation)
        {
            if (context.CanDelete == false)
            {
                return Unauthorized();
            }

            pageNavigation = WikiUtility.CleanPartialURI(pageNavigation);
            fileNavigation = WikiUtility.CleanPartialURI(fileNavigation);

            PageFileRepository.DeletePageFileByPageNavigationAndFileName(pageNavigation, fileNavigation);

            return Content("Success");
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
                var img = SixLabors.ImageSharp.Image.Load(new MemoryStream(file.Data));

                string contentType = file.ContentType;

                ImageFormat format;
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
                    case "image/tiff":
                        format = ImageFormat.Tiff;
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
                    using var bmp = Images.ResizeImage(img, width, height);
                    using MemoryStream ms = new MemoryStream();
                    ChangeImageType(bmp, format, ms);
                    return File(ms.ToArray(), contentType);
                }
                else
                {
                    using MemoryStream ms = new MemoryStream();
                    ChangeImageType(img, format, ms);
                    return File(ms.ToArray(), contentType);
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
                var img = SixLabors.ImageSharp.Image.Load(new MemoryStream(file.Data));

                int iscale = int.Parse(scale);
                if (iscale != 100)
                {
                    int width = (int)(img.Width * (iscale / 100.0));
                    int height = (int)(img.Height * (iscale / 100.0));

                    using var bmp = Images.ResizeImage(img, width, height);
                    using MemoryStream ms = new MemoryStream();
                    bmp.SaveAsPng(ms);
                    return File(ms.ToArray(), "image/png");
                }
                else
                {
                    using MemoryStream ms = new MemoryStream();
                    img.SaveAsPng(ms);
                    return File(ms.ToArray(), "image/png");
                }
            }
            else
            {
                return NotFound($"[{fileNavigation}] was not found on the page [{pageNavigation}].");
            }
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
