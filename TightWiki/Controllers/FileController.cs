using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using TightWiki.Controllers;
using TightWiki.Shared;
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
        private IWebHostEnvironment _env;

        public FileController(IWebHostEnvironment env)
        {
            _env = env;
        }

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
        public ActionResult Emoji(string pageNavigation)
        {
            if (context.CanView == false)
            {
                return Unauthorized();
            }

            if (string.IsNullOrEmpty(pageNavigation) == false)
            {
                string shortcut = $"::{pageNavigation.ToLower()}::";
                var emoji = Global.Emojis.Where(o => o.Shortcut == shortcut).FirstOrDefault();
                if (emoji != null)
                {
                    emoji.ImageData = Cache.Get<byte[]>($"Emoji:{shortcut}");
                    if (emoji.ImageData == null)
                    {
                        //We dont get the bytes by default, that would be alot of RAM for all the thousandas of images.
                        emoji.ImageData = EmojiRepository.GetEmojiByName(emoji.Name)?.ImageData;
                        Cache.Put($"Emoji:{shortcut}", emoji.ImageData);
                    }

                    if (emoji.ImageData != null)
                    {
                        string scale = Request.Query["Scale"].ToString().IsNullOrEmpty("100");
                        var img = SixLabors.ImageSharp.Image.Load(new MemoryStream(emoji.ImageData));

                        int iscale = int.Parse(scale);
                        int defaultHeight = Global.DefaultEmojiHeight;
                        int height = img.Height;
                        int width = img.Width;

                        //Get the difference in between the default image height and the image height.
                        int difference = height - defaultHeight;

                        //Apply the new size to the height and width to keep the ratio the same.
                        height -= difference;
                        width -= difference;

                        //Adjust to any specified scaling.
                        height = (int)(height * (iscale / 100.0));
                        width = (int)(width * (iscale / 100.0));

                        //Adjusting by a ratio (and especially after applying additional scaling) may have caused one
                        //  deminsion to become very small (or even negative). So here we will check the height and width
                        //  to ensure they are both at least n pixels and adjust both demensions.
                        if (height < 16)
                        {
                            difference = 16 - height;
                            height += difference;
                            width += difference;
                        }
                        if (width < 16)
                        {
                            difference = 16 - width;
                            height += difference;
                            width += difference;
                        }

                        if (emoji.MimeType?.ToLower() == "image/gif")
                        {
                            var imageBytes = Images.ResizeImageBytes(emoji.ImageData, width, height);
                            //For the time begin, we do not support resizing gif animations.
                            return File(imageBytes, emoji.MimeType);
                        }
                        else
                        {
                            using var bmp = Images.ResizeImage(img, width, height);
                            using MemoryStream ms = new MemoryStream();
                            bmp.SaveAsPng(ms);
                            return File(ms.ToArray(), "image/png");
                        }
                    }
                }
            }

            return NotFound($"Emoji {pageNavigation} was not found");
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
