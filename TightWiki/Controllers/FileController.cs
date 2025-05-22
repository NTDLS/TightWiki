using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using NTDLS.Helpers;
using SixLabors.ImageSharp;
using System.Web;
using TightWiki.Caching;
using TightWiki.Library;
using TightWiki.Models;
using TightWiki.Models.DataModels;
using TightWiki.Models.ViewModels.File;
using TightWiki.Repository;
using static TightWiki.Library.Images;

namespace TightWiki.Controllers
{
    [Route("File")]
    public class FileController(
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IStringLocalizer<ProfileController> localizer)
        : WikiControllerBase(signInManager, userManager)
    {
        /// <summary>
        /// Gets an image attached to a page.
        /// </summary>
        /// <param name="givenPageNavigation">The navigation link of the page.</param>
        /// <param name="givenFileNavigation">The navigation link of the file.</param>
        /// <param name="fileRevision">The revision of the the file (NOT THE PAGE REVISION).</param>
        [HttpGet("Image/{givenPageNavigation}/{givenFileNavigation}/{fileRevision:int?}")]
        public ActionResult Image(string givenPageNavigation, string givenFileNavigation, int? fileRevision = null)
        {
            var pageNavigation = new NamespaceNavigation(givenPageNavigation);
            var fileNavigation = new NamespaceNavigation(givenFileNavigation);

            int givenScale = GetQueryValue("Scale", 100);

            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Page, [givenPageNavigation, givenFileNavigation, fileRevision, givenScale]);
            if (WikiCache.TryGet<ImageCacheItem>(cacheKey, out var cached))
            {
                return File(cached.Bytes, cached.ContentType);
            }

            var file = PageFileRepository.GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(pageNavigation.Canonical, fileNavigation.Canonical, fileRevision);
            if (file != null)
            {
                if (file.ContentType == "image/x-icon")
                {
                    //We do not handle the resizing of icon file. Maybe later....
                    return File(file.Data, file.ContentType);
                }

                var img = SixLabors.ImageSharp.Image.Load(new MemoryStream(file.Data));

                if (givenScale > 500)
                {
                    givenScale = 500;
                }
                if (givenScale != 100)
                {
                    int width = (int)(img.Width * (givenScale / 100.0));
                    int height = (int)(img.Height * (givenScale / 100.0));

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

                    if (file.ContentType.Equals("image/gif", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var resized = ResizeGifImage(file.Data, width, height);
                        return File(resized, "image/gif");
                    }
                    else
                    {
                        using var image = ResizeImage(img, width, height);
                        using var ms = new MemoryStream();
                        file.ContentType = BestEffortConvertImage(image, ms, file.ContentType);
                        var cacheItem = new ImageCacheItem(ms.ToArray(), file.ContentType);
                        WikiCache.Put(cacheKey, cacheItem);
                        return File(cacheItem.Bytes, cacheItem.ContentType);
                    }
                }
                else
                {
                    return File(file.Data, file.ContentType);
                }
            }
            else
            {
                return NotFound(String.Format(localizer["[{0}] was not found on the page [{1}]."].Value, fileNavigation, pageNavigation));
            }
        }

        /// <summary>
        /// Gets an image from the database, converts it to a PNG with optional scaling and returns it to the client.
        /// </summary>
        /// <param name="givenPageNavigation">The navigation link of the page.</param>
        /// <param name="givenFileNavigation">The navigation link of the file.</param>
        /// <param name="fileRevision">The revision of the the FILE (NOT THE PAGE REVISION)</param>
        [AllowAnonymous]
        [HttpGet("Png/{givenPageNavigation}/{givenFileNavigation}/{fileRevision:int?}")]
        public ActionResult Png(string givenPageNavigation, string givenFileNavigation, int? fileRevision = null)
        {
            SessionState.RequireViewPermission();

            var pageNavigation = new NamespaceNavigation(givenPageNavigation);
            var fileNavigation = new NamespaceNavigation(givenFileNavigation);

            int givenScale = GetQueryValue("Scale", 100);

            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Page, [givenPageNavigation, givenFileNavigation, fileRevision, givenScale]);
            if (WikiCache.TryGet<ImageCacheItem>(cacheKey, out var cached))
            {
                return File(cached.Bytes, cached.ContentType);
            }

            var file = PageFileRepository.GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(pageNavigation.Canonical, fileNavigation.Canonical, fileRevision);
            if (file != null)
            {
                var img = SixLabors.ImageSharp.Image.Load(new MemoryStream(Utility.Decompress(file.Data)));

                if (givenScale > 500)
                {
                    givenScale = 500;
                }

                if (givenScale != 100)
                {
                    int width = (int)(img.Width * (givenScale / 100.0));
                    int height = (int)(img.Height * (givenScale / 100.0));

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
                    return File(cacheItem.Bytes, cacheItem.ContentType);
                }
                else
                {
                    using var ms = new MemoryStream();
                    img.SaveAsPng(ms);

                    var cacheItem = new ImageCacheItem(ms.ToArray(), "image/png");
                    WikiCache.Put(cacheKey, cacheItem);
                    return File(cacheItem.Bytes, cacheItem.ContentType);
                }
            }
            else
            {
                return NotFound(String.Format(localizer["[{0}] was not found on the page [{1}]."].Value, fileNavigation, pageNavigation));
            }
        }

        /// <summary>
        /// Gets a file from the database and returns it to the client. This works even
        /// when the file or file revision is not even attached to the page anymore.
        /// <param name="givenPageNavigation">The navigation link of the page.</param>
        /// <param name="givenFileNavigation">The navigation link of the file.</param>
        /// <param name="fileRevision">The revision of the the FILE (NOT THE PAGE REVISION),</param>
        /// </summary>
        [AllowAnonymous]
        [HttpGet("Binary/{givenPageNavigation}/{givenFileNavigation}/{fileRevision:int?}")]
        public ActionResult Binary(string givenPageNavigation, string givenFileNavigation, int? fileRevision = null)
        {
            SessionState.RequireViewPermission();

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
                return NotFound(String.Format(localizer["[{0}] was not found on the page [{1}]."].Value, fileNavigation, pageNavigation));
            }
        }

        /// <summary>
        /// Populate the upload page. Shows the attachments.
        /// </summary>
        [Authorize]
        [HttpGet("Revisions/{givenPageNavigation}/{givenFileNavigation}")]
        public ActionResult Revisions(string givenPageNavigation, string givenFileNavigation)
        {
            SessionState.RequireViewPermission();

            var pageNavigation = new NamespaceNavigation(givenPageNavigation);
            var fileNavigation = new NamespaceNavigation(givenFileNavigation);

            var model = new PageFileRevisionsViewModel()
            {
                PageNavigation = pageNavigation.Canonical,
                FileNavigation = fileNavigation.Canonical,
                Revisions = PageFileRepository.GetPageFileAttachmentRevisionsByPageAndFileNavigationPaged
                    (pageNavigation.Canonical, fileNavigation.Canonical, GetQueryValue("page", 1))
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
            SessionState.RequireCreatePermission();

            var pageNavigation = new NamespaceNavigation(givenPageNavigation);

            var page = PageRepository.GetPageRevisionByNavigation(pageNavigation);
            if (page != null)
            {
                var pageFiles = PageFileRepository.GetPageFilesInfoByPageId(page.Id);

                return View(new FileAttachmentViewModel
                {
                    PageNavigation = page.Navigation,
                    PageRevision = page.Revision,
                    Files = pageFiles
                });
            }

            return View(new FileAttachmentViewModel
            {
                Files = new()
            });
        }

        /// <summary>
        /// Uploads a file by drag drop.
        /// </summary>
        [Authorize]
        [HttpPost("UploadDragDrop/{givenPageNavigation}")]
        public IActionResult UploadDragDrop(string givenPageNavigation, List<IFormFile> postedFiles)
        {
            SessionState.RequireCreatePermission();

            try
            {
                var pageNavigation = new NamespaceNavigation(givenPageNavigation);

                var page = PageRepository.GetPageInfoByNavigation(pageNavigation.Canonical).EnsureNotNull();

                foreach (IFormFile file in postedFiles)
                {
                    if (file != null)
                    {
                        var fileSize = file.Length;
                        if (fileSize > 0)
                        {
                            if (fileSize > GlobalConfiguration.MaxAttachmentFileSize)
                            {
                                return Json(new { message = String.Format(localizer["Could not attach file: [{0}], too large."].Value, file.FileName) });
                            }

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
                            }, (SessionState.Profile?.UserId).EnsureNotNullOrEmpty());
                        }
                    }
                }
                return Json(new { success = true, message = String.Format(localizer["files: {0:n0}"].Value, postedFiles.Count) });
            }
            catch (Exception ex)
            {
                ExceptionRepository.InsertException(ex, "Failed to upload file.");
                return StatusCode(500, new { success = false, message = String.Format(localizer["An error occurred: {0}"].Value, ex.Message) });
            }
        }

        /// <summary>
        /// Uploads a file by manually selecting it for upload.
        /// </summary>
        [Authorize]
        [HttpPost("ManualUpload/{givenPageNavigation}")]
        public IActionResult ManualUpload(string givenPageNavigation, IFormFile fileData)
        {
            SessionState.RequireCreatePermission();

            var pageNavigation = new NamespaceNavigation(givenPageNavigation);

            var page = PageRepository.GetPageInfoByNavigation(pageNavigation.Canonical).EnsureNotNull();

            if (fileData != null)
            {
                var fileSize = fileData.Length;
                if (fileSize > 0)
                {
                    if (fileSize > GlobalConfiguration.MaxAttachmentFileSize)
                    {
                        return Content(localizer["Could not save the attached file, too large"].Value);
                    }

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
                    }, (SessionState.Profile?.UserId).EnsureNotNullOrEmpty());

                    return Content(localizer["Success"].Value);
                }
            }

            return Content(localizer["Failure"].Value);
        }

        /// <summary>
        /// Allows a user to delete a page attachment from a page.
        /// </summary>
        [HttpPost("Detach/{givenPageNavigation}/{givenFileNavigation}/{pageRevision}")]
        public ActionResult Detach(string givenPageNavigation, string givenFileNavigation, int pageRevision)
        {
            SessionState.RequireDeletePermission();

            PageFileRepository.DetachPageRevisionAttachment(
                new NamespaceNavigation(givenPageNavigation).Canonical,
                new NamespaceNavigation(givenFileNavigation).Canonical, pageRevision);

            return Content(localizer["Success"].Value);
        }


        /// <summary>
        /// Gets a file from the database, converts it to a PNG with optional scaling and returns it to the client.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("Emoji/{givenPageNavigation}")]
        public ActionResult Emoji(string givenPageNavigation)
        {
            SessionState.RequireViewPermission();

            var pageNavigation = Navigation.Clean(givenPageNavigation);

            if (string.IsNullOrEmpty(pageNavigation) == false)
            {
                string scale = GetQueryValue("Scale", "100");

                string shortcut = $"%%{pageNavigation.ToLowerInvariant()}%%";
                var emoji = GlobalConfiguration.Emojis.Where(o => o.Shortcut == shortcut).FirstOrDefault();
                if (emoji != null)
                {
                    //Do we have this scale cached already?
                    var scaledImageCacheKey = WikiCacheKey.Build(WikiCache.Category.Emoji, [shortcut, scale]);
                    if (WikiCache.TryGet<ImageCacheItem>(scaledImageCacheKey, out var cachedEmoji))
                    {
                        return File(cachedEmoji.Bytes, cachedEmoji.ContentType);
                    }

                    var imageCacheKey = WikiCacheKey.Build(WikiCache.Category.Emoji, [shortcut]);
                    emoji.ImageData = WikiCache.Get<byte[]>(imageCacheKey);
                    if (emoji.ImageData == null)
                    {
                        //We don't get the bytes by default, that would be a lot of RAM for all the thousands of images.
                        emoji.ImageData = EmojiRepository.GetEmojiByName(emoji.Name)?.ImageData;

                        if (emoji.ImageData == null)
                        {
                            return NotFound(String.Format(localizer["Emoji {0} was not found"].Value, pageNavigation));
                        }

                        WikiCache.Put(imageCacheKey, emoji.ImageData);
                    }

                    if (emoji.ImageData != null)
                    {
                        var decompressedImageBytes = Utility.Decompress(emoji.ImageData);

                        var img = SixLabors.ImageSharp.Image.Load(new MemoryStream(decompressedImageBytes));

                        int customScalePercent = int.Parse(scale);
                        if (customScalePercent > 500)
                        {
                            customScalePercent = 500;
                        }

                        var (Width, Height) = Utility.ScaleToMaxOf(img.Width, img.Height, GlobalConfiguration.DefaultEmojiHeight);

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

                        if (emoji.MimeType?.ToLowerInvariant() == "image/gif")
                        {
                            var resized = ResizeGifImage(decompressedImageBytes, Width, Height);
                            var itemCache = new ImageCacheItem(resized, "image/gif");
                            WikiCache.Put(scaledImageCacheKey, itemCache);
                            return File(itemCache.Bytes, itemCache.ContentType);
                        }
                        else
                        {
                            using var image = Images.ResizeImage(img, Width, Height);
                            using var ms = new MemoryStream();
                            image.SaveAsPng(ms);

                            var itemCache = new ImageCacheItem(ms.ToArray(), "image/png");
                            WikiCache.Put(scaledImageCacheKey, itemCache);

                            return File(itemCache.Bytes, itemCache.ContentType);
                        }
                    }
                }
            }

            return NotFound(String.Format(localizer["Emoji {0} was not found"].Value, pageNavigation));
        }
    }
}
