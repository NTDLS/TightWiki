using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using System.Web;
using TightWiki.Library;
using TightWiki.Models.DataModels;
using TightWiki.Models.ViewModels.File;
using TightWiki.Repository;
using static TightWiki.Library.Images;

namespace TightWiki.Controllers
{
    [Route("File")]
    public class FileController : ControllerBase
    {
        public FileController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
            : base(signInManager, userManager)
        {
        }

        /// <summary>
        /// Gets an image attached to a page.
        /// </summary>
        /// <param name="givenPageNavigation">The navigation link of the page.</param>
        /// <param name="givenfileNavigation">The navigation link of the file.</param>
        /// <param name="fileRevision">The revision of the the file (NOT THE PAGE REVISION).</param>
        /// <returns></returns>
        [HttpGet("Image/{givenPageNavigation}/{givenfileNavigation}/{fileRevision:int?}")]
        public ActionResult Image(string givenPageNavigation, string givenfileNavigation, int? fileRevision = null)
        {
            var pageNavigation = new NamespaceNavigation(givenPageNavigation);
            var fileNavigation = new NamespaceNavigation(givenfileNavigation);

            string scale = GetQueryString("Scale", "100");
            var file = PageFileRepository.GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(pageNavigation.Canonical, fileNavigation.Canonical, fileRevision);

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
                    case "image/gif":
                        format = ImageFormat.Gif;
                        break;
                    case "image/tiff":
                        format = ImageFormat.Tiff;
                        break;
                    default:
                        contentType = "image/png";
                        format = ImageFormat.Gif;
                        break;
                }

                int iscale = int.Parse(scale);
                if (iscale > 500)
                {
                    iscale = 500;
                }
                if (iscale != 100)
                {
                    int width = (int)(img.Width * (iscale / 100.0));
                    int height = (int)(img.Height * (iscale / 100.0));

                    //Adjusting by a ratio (and especially after applying additional scaling) may have caused one
                    //  deminsion to become very small (or even negative). So here we will check the height and width
                    //  to ensure they are both at least n pixels and adjust both demensions.
                    if (height < 16)
                    {
                        int difference = 16 - height;
                        height += difference;
                        width += difference;
                    }
                    if (width < 16)
                    {
                        int difference = 16 - width;
                        height += difference;
                        width += difference;
                    }

                    using var image = Images.ResizeImage(img, width, height);
                    using var ms = new MemoryStream();
                    ChangeImageType(image, format, ms);
                    return File(ms.ToArray(), contentType);
                }
                else
                {
                    using var ms = new MemoryStream();
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
        /// Gets an image from the database, converts it to a PNG with optional scaling and returns it to the client.
        /// </summary>
        /// <param name="givenPageNavigation">The navigation link of the page.</param>
        /// <param name="givenfileNavigation">The navigation link of the file.</param>
        /// <param name="fileRevision">The revision of the the FILE (NOT THE PAGE REVISION)</param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("Png/{givenPageNavigation}/{givenfileNavigation}/{fileRevision:int?}")]
        public ActionResult Png(string givenPageNavigation, string givenfileNavigation, int? fileRevision = null)
        {
            WikiContext.RequireViewPermission();

            var pageNavigation = new NamespaceNavigation(givenPageNavigation);
            var fileNavigation = new NamespaceNavigation(givenfileNavigation);

            string scale = GetQueryString("Scale", "100");

            var file = PageFileRepository.GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(pageNavigation.Canonical, fileNavigation.Canonical, fileRevision);
            if (file != null)
            {
                var img = SixLabors.ImageSharp.Image.Load(new MemoryStream(Utility.Decompress(file.Data)));

                int iscale = int.Parse(scale);
                if (iscale > 500)
                {
                    iscale = 500;
                }

                if (iscale != 100)
                {
                    int width = (int)(img.Width * (iscale / 100.0));
                    int height = (int)(img.Height * (iscale / 100.0));

                    //Adjusting by a ratio (and especially after applying additional scaling) may have caused one
                    //  deminsion to become very small (or even negative). So here we will check the height and width
                    //  to ensure they are both at least n pixels and adjust both demensions.
                    if (height < 16)
                    {
                        int difference = 16 - height;
                        height += difference;
                        width += difference;
                    }
                    if (width < 16)
                    {
                        int difference = 16 - width;
                        height += difference;
                        width += difference;
                    }

                    using var image = Images.ResizeImage(img, width, height);
                    using var ms = new MemoryStream();
                    image.SaveAsPng(ms);
                    return File(ms.ToArray(), "image/png");
                }
                else
                {
                    using var ms = new MemoryStream();
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
        /// Gets a file from the database and returns it to the client.
        /// <param name="givenPageNavigation">The navigation link of the page.</param>
        /// <param name="givenfileNavigation">The navigation link of the file.</param>
        /// <param name="fileRevision">The revision of the the FILE (NOT THE PAGE REVISION),</param>
        /// </summary>
        [AllowAnonymous]
        [HttpGet("Binary/{givenPageNavigation}/{givenfileNavigation}/{fileRevision:int?}")]
        public ActionResult Binary(string givenPageNavigation, string givenfileNavigation, int? fileRevision = null)
        {
            WikiContext.RequireViewPermission();

            var pageNavigation = new NamespaceNavigation(givenPageNavigation);
            var fileNavigation = new NamespaceNavigation(givenfileNavigation);

            var file = PageFileRepository.GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(pageNavigation.Canonical, fileNavigation.Canonical, fileRevision);

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
        /// Populate the upload page. Shows the attachments.
        /// </summary>
        [Authorize]
        [HttpGet("EditPageAttachment/{givenPageNavigation}")]
        public ActionResult EditPageAttachment(string givenPageNavigation)
        {
            WikiContext.RequireCreatePermission();

            var pageNavigation = new NamespaceNavigation(givenPageNavigation);

            var page = PageRepository.GetPageRevisionByNavigation(pageNavigation);
            if (page != null)
            {
                var pageFiles = PageFileRepository.GetPageFilesInfoByPageIdAndPageRevision(page.Id);

                return View(new FileAttachmentViewModel()
                {
                    Files = pageFiles
                });
            }

            return View(new FileAttachmentViewModel()
            {
                Files = new List<PageFileAttachment>()
            });
        }

        /// <summary>
        /// Uploads a file by drag drop.
        /// </summary>
        [Authorize]
        [HttpPost("UploadDragDrop/{givenPageNavigation}")]
        public IActionResult UploadDragDrop(string givenPageNavigation, List<IFormFile> postedFiles)
        {
            WikiContext.RequireCreatePermission();

            var pageNavigation = new NamespaceNavigation(givenPageNavigation);

            var page = PageRepository.GetPageInfoByNavigation(pageNavigation.Canonical).EnsureNotNull();

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
                            FileNavigation = Navigation.Clean(fileName),
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
        [HttpPost("ManualUpload/{givenPageNavigation}")]
        public IActionResult ManualUpload(string givenPageNavigation, IFormFile fileData)
        {
            WikiContext.RequireCreatePermission();

            var pageNavigation = new NamespaceNavigation(givenPageNavigation);

            var page = PageRepository.GetPageInfoByNavigation(pageNavigation.Canonical).EnsureNotNull();

            if (fileData != null)
            {
                var fileSize = fileData.Length;
                if (fileSize > 0)
                {
                    var fileName = HttpUtility.UrlDecode(fileData.FileName);

                    PageFileRepository.UpsertPageFile(new PageFileAttachment()
                    {
                        Data = Utility.ConvertHttpFileToBytes(fileData),
                        CreatedDate = DateTime.UtcNow,
                        PageId = page.Id,
                        Name = fileName,
                        FileNavigation = Navigation.Clean(fileName),
                        Size = fileSize,
                        ContentType = Utility.GetMimeType(fileName)
                    }); ;

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
        [HttpPost("Delete/{givenPageNavigation}/{givenfileNavigation}")]
        public ActionResult Delete(string givenPageNavigation, string givenfileNavigation)
        {
            WikiContext.RequireDeletePermission();

            var pageNavigation = new NamespaceNavigation(givenPageNavigation);
            var fileNavigation = new NamespaceNavigation(givenfileNavigation);

            PageFileRepository.DeletePageFileByPageNavigationAndFileName(pageNavigation.Canonical, fileNavigation.Canonical);

            return Content("Success");
        }


        /// <summary>
        /// Gets a file from the database, converts it to a PNG with optional scaling and returns it to the client.
        /// </summary>
        /// <param name="navigation"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("Emoji/{givenPageNavigation}")]
        public ActionResult Emoji(string givenPageNavigation)
        {
            WikiContext.RequireViewPermission();

            var pageNavigation = Navigation.Clean(givenPageNavigation);

            if (string.IsNullOrEmpty(pageNavigation) == false)
            {
                string shortcut = $"%%{pageNavigation.ToLower()}%%";
                var emoji = GlobalSettings.Emojis.Where(o => o.Shortcut == shortcut).FirstOrDefault();
                if (emoji != null)
                {
                    var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Emoji, [shortcut]);
                    emoji.ImageData = WikiCache.Get<byte[]>(cacheKey);
                    if (emoji.ImageData == null)
                    {
                        //We dont get the bytes by default, that would be alot of RAM for all the thousandas of images.
                        emoji.ImageData = EmojiRepository.GetEmojiByName(emoji.Name)?.ImageData;
                        if (emoji.ImageData != null)
                        {
                            WikiCache.Put(cacheKey, emoji.ImageData);
                        }
                    }

                    if (emoji.ImageData != null)
                    {
                        string scale = GetQueryString("Scale", "100");
                        var img = SixLabors.ImageSharp.Image.Load(new MemoryStream(Utility.Decompress(emoji.ImageData)));

                        int iscale = int.Parse(scale);
                        if (iscale > 500)
                        {
                            iscale = 500;
                        }

                        int defaultHeight = GlobalSettings.DefaultEmojiHeight;
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
                            using var image = Images.ResizeImage(img, width, height);
                            using var ms = new MemoryStream();
                            image.SaveAsGif(ms);
                            return File(ms.ToArray(), "image/gif");
                        }
                        else
                        {
                            using var image = Images.ResizeImage(img, width, height);
                            using var ms = new MemoryStream();
                            image.SaveAsPng(ms);
                            return File(ms.ToArray(), "image/png");
                        }
                    }
                }
            }

            return NotFound($"Emoji {pageNavigation} was not found");
        }
    }
}
