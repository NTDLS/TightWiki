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
    public class FileController : WikiControllerBase
    {
        public FileController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
            : base(signInManager, userManager)
        {
        }




        /// <summary>
        /// Gets an image attached to a page.
        /// </summary>
        /// <param name="givenPageNavigation">The navigation link of the page.</param>
        /// <param name="givenFileNavigation">The navigation link of the file.</param>
        /// <param name="fileRevision">The revision of the the file (NOT THE PAGE REVISION).</param>
        /// <returns></returns>
        [HttpGet("Image/{givenPageNavigation}/{givenFileNavigation}/{fileRevision:int?}")]
        public ActionResult Image(string givenPageNavigation, string givenFileNavigation, int? fileRevision = null)
        {
            var pageNavigation = new NamespaceNavigation(givenPageNavigation);
            var fileNavigation = new NamespaceNavigation(givenFileNavigation);

            string givenScale = GetQueryString("Scale", "100");

            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Page, [givenPageNavigation, givenFileNavigation, fileRevision, givenScale]);
            if (WikiCache.TryGet<PageFileAttachment>(cacheKey, out var cached))
            {
                return File(cached.Data, cached.ContentType);
            }

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
                        format = ImageFormat.Png;
                        break;
                }

                int parsedScale = int.Parse(givenScale);
                if (parsedScale > 500)
                {
                    parsedScale = 500;
                }
                if (parsedScale != 100)
                {
                    int width = (int)(img.Width * (parsedScale / 100.0));
                    int height = (int)(img.Height * (parsedScale / 100.0));

                    //Adjusting by a ratio (and especially after applying additional scaling) may have caused one
                    //  dimension to become very small (or even negative). So here we will check the height and width
                    //  to ensure they are both at least n pixels and adjust both dimensions.
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

                    var cacheItem = new ImageCacheItem(ms.ToArray(), contentType);
                    WikiCache.Put(cacheKey, cacheItem);
                    return File(cacheItem.Data, cacheItem.ContentType);
                }
                else
                {
                    using var ms = new MemoryStream();
                    ChangeImageType(img, format, ms);

                    var cacheItem = new ImageCacheItem(ms.ToArray(), contentType);
                    WikiCache.Put(cacheKey, cacheItem);
                    return File(cacheItem.Data, cacheItem.ContentType);
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
        /// <param name="givenFileNavigation">The navigation link of the file.</param>
        /// <param name="fileRevision">The revision of the the FILE (NOT THE PAGE REVISION)</param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("Png/{givenPageNavigation}/{givenFileNavigation}/{fileRevision:int?}")]
        public ActionResult Png(string givenPageNavigation, string givenFileNavigation, int? fileRevision = null)
        {
            WikiContext.RequireViewPermission();

            var pageNavigation = new NamespaceNavigation(givenPageNavigation);
            var fileNavigation = new NamespaceNavigation(givenFileNavigation);

            string givenScale = GetQueryString("Scale", "100");

            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Page, [givenPageNavigation, givenFileNavigation, fileRevision, givenScale]);
            if (WikiCache.TryGet<PageFileAttachment>(cacheKey, out var cached))
            {
                return File(cached.Data, cached.ContentType);
            }


            var file = PageFileRepository.GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(pageNavigation.Canonical, fileNavigation.Canonical, fileRevision);
            if (file != null)
            {
                var img = SixLabors.ImageSharp.Image.Load(new MemoryStream(Utility.Decompress(file.Data)));

                int parsedScale = int.Parse(givenScale);
                if (parsedScale > 500)
                {
                    parsedScale = 500;
                }

                if (parsedScale != 100)
                {
                    int width = (int)(img.Width * (parsedScale / 100.0));
                    int height = (int)(img.Height * (parsedScale / 100.0));

                    //Adjusting by a ratio (and especially after applying additional scaling) may have caused one
                    //  dimension to become very small (or even negative). So here we will check the height and width
                    //  to ensure they are both at least n pixels and adjust both dimensions.
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

                    var cacheItem = new ImageCacheItem(ms.ToArray(), "image/png");
                    WikiCache.Put(cacheKey, cacheItem);
                    return File(cacheItem.Data, cacheItem.ContentType);
                }
                else
                {
                    using var ms = new MemoryStream();
                    img.SaveAsPng(ms);

                    var cacheItem = new ImageCacheItem(ms.ToArray(), "image/png");
                    WikiCache.Put(cacheKey, cacheItem);
                    return File(cacheItem.Data, cacheItem.ContentType);
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
        /// <param name="givenFileNavigation">The navigation link of the file.</param>
        /// <param name="fileRevision">The revision of the the FILE (NOT THE PAGE REVISION),</param>
        /// </summary>
        [AllowAnonymous]
        [HttpGet("Binary/{givenPageNavigation}/{givenFileNavigation}/{fileRevision:int?}")]
        public ActionResult Binary(string givenPageNavigation, string givenFileNavigation, int? fileRevision = null)
        {
            WikiContext.RequireViewPermission();

            var pageNavigation = new NamespaceNavigation(givenPageNavigation);
            var fileNavigation = new NamespaceNavigation(givenFileNavigation);

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
        [HttpGet("Revisions/{givenPageNavigation}/{givenFileNavigation}")]
        public ActionResult Revisions(string givenPageNavigation, string givenFileNavigation)
        {
            WikiContext.RequireViewPermission();

            var pageNavigation = new NamespaceNavigation(givenPageNavigation);
            var fileNavigation = new NamespaceNavigation(givenFileNavigation);

            var model = new PageFileRevisionsViewModel()
            {
                PageNavigation = pageNavigation.Canonical,
                FileNavigation = fileNavigation.Canonical,
                Revisions = PageFileRepository.GetPageFileAttachmentRevisionsByPageAndFileNavigationPaged
                    (pageNavigation.Canonical, fileNavigation.Canonical, GetQueryString("page", 1))
            };

            model.PaginationPageCount = (model.Revisions.FirstOrDefault()?.PaginationPageCount ?? 0);

            return View(model);
        }

        /// <summary>
        /// Populate the upload page. Shows the attachments.
        /// </summary>
        [Authorize]
        [HttpGet("PageAttachments/{givenPageNavigation}")]
        public ActionResult PageAttachments(string givenPageNavigation)
        {
            WikiContext.RequireCreatePermission();

            var pageNavigation = new NamespaceNavigation(givenPageNavigation);

            var page = PageRepository.GetPageRevisionByNavigation(pageNavigation);
            if (page != null)
            {
                var pageFiles = PageFileRepository.GetPageFilesInfoByPageId(page.Id);

                return View(new FileAttachmentViewModel()
                {
                    Files = pageFiles
                });
            }

            return View(new FileAttachmentViewModel()
            {
                Files = new List<PageFileAttachmentInfo>()
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
                        }, (WikiContext.Profile?.UserId).EnsureNotNullOrEmpty());

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
                    }, (WikiContext.Profile?.UserId).EnsureNotNullOrEmpty());

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
        [HttpPost("Delete/{givenPageNavigation}/{givenFileNavigation}")]
        public ActionResult Delete(string givenPageNavigation, string givenFileNavigation)
        {
            WikiContext.RequireDeletePermission();

            var pageNavigation = new NamespaceNavigation(givenPageNavigation);
            var fileNavigation = new NamespaceNavigation(givenFileNavigation);

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
                        //We don't get the bytes by default, that would be a lot of RAM for all the thousands of images.
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

                        int customScalePercent = int.Parse(scale);
                        if (customScalePercent > 500)
                        {
                            customScalePercent = 500;
                        }

                        var (Width, Height) = Utility.ScaleToMaxOf(img.Width, img.Height, GlobalSettings.DefaultEmojiHeight);

                        //Adjust to any specified scaling.
                        Height = (int)(Height * (customScalePercent / 100.0));
                        Width = (int)(Width * (customScalePercent / 100.0));

                        //Adjusting by a ratio (and especially after applying additional scaling) may have caused one
                        //  dimension to become very small (or even negative). So here we will check the height and width
                        //  to ensure they are both at least n pixels and adjust both dimensions.
                        if (Height < 16)
                        {
                            Height += 16 - Height;
                            Width += 16 - Height;
                        }
                        if (Width < 16)
                        {
                            Height += 16 - Width;
                            Width += 16 - Width;
                        }

                        if (emoji.MimeType?.ToLower() == "image/gif")
                        {
                            using var image = Images.ResizeImage(img, Width, Height);
                            using var ms = new MemoryStream();
                            image.SaveAsGif(ms);
                            return File(ms.ToArray(), "image/gif");
                        }
                        else
                        {
                            using var image = Images.ResizeImage(img, Width, Height);
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
