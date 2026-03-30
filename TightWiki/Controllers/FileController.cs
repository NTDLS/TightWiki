using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NTDLS.Helpers;
using SixLabors.ImageSharp;
using System.Web;
using TightWiki.Plugin;
using TightWiki.Plugin.Caching;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Library;
using TightWiki.Plugin.Models;
using TightWiki.Repository;
using TightWiki.ViewModels.File;
using static TightWiki.Plugin.Library.TwImages;

namespace TightWiki.Controllers
{
    [Route("File")]
    public class FileController
        : WikiControllerBase<FileController>
    {
        private readonly ILogger<ITwEngine> _logger;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public FileController(ILogger<ITwEngine> logger, SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager, ITwSharedLocalizationText localizer, TwConfiguration wikiConfiguration)
            : base(logger, signInManager, userManager, localizer, wikiConfiguration)
        {
            _logger = logger;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        /// <summary>
        /// Gets an image attached to a page.
        /// </summary>
        /// <param name="givenPageNavigation">The navigation link of the page.</param>
        /// <param name="givenFileNavigation">The navigation link of the file.</param>
        /// <param name="fileRevision">The revision of the the file (NOT THE PAGE REVISION).</param>
        [HttpGet("Image/{givenPageNavigation}/{givenFileNavigation}/{fileRevision:int?}")]
        public async Task<ActionResult> Image(string givenPageNavigation, string givenFileNavigation, int? fileRevision = null)
        {
            try
            {
                var pageNavigation = new TwNamespaceNavigation(givenPageNavigation);
                var fileNavigation = new TwNamespaceNavigation(givenFileNavigation);

                var scale = GetQueryValue<int?>("Scale");
                var maxWidth = GetQueryValue<int?>("MaxWidth");

                var cacheKey = TwCacheKeyFunction.Build(TwCache.Category.Page, [givenPageNavigation, givenFileNavigation, fileRevision, scale, maxWidth]);
                if (TwCache.TryGet<TwImageCacheItem>(cacheKey, out var cached))
                {
                    return File(cached.Bytes, cached.ContentType);
                }

                var file = await PageFileRepository.GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(pageNavigation.Canonical, fileNavigation.Canonical, fileRevision);
                if (file != null)
                {
                    if (file.ContentType == "image/x-icon")
                    {
                        //We do not handle the resizing of icon file. Maybe later....
                        return File(file.Data, file.ContentType);
                    }

                    var img = SixLabors.ImageSharp.Image.Load(new MemoryStream(file.Data));

                    if (scale > 500)
                    {
                        scale = 500;
                    }
                    //Enforce scale if specified.
                    if (scale != null && scale != 100)
                    {
                        int width = (int)(img.Width * (scale / 100.0));
                        int height = (int)(img.Height * (scale / 100.0));

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
                            var cacheItem = new TwImageCacheItem(ms.ToArray(), file.ContentType);
                            TwCache.Set(cacheKey, cacheItem);
                            return File(cacheItem.Bytes, cacheItem.ContentType);
                        }
                    }
                    //Enforce max width if specified.
                    else if (maxWidth > 0 && img.Width > maxWidth)
                    {
                        double widthScale = (double)maxWidth / img.Width;

                        int width = Math.Max(1, (int)Math.Round(img.Width * widthScale));
                        int height = Math.Max(1, (int)Math.Round(img.Height * widthScale));

                        using var image = ResizeImage(img, width, height);
                        using var ms = new MemoryStream();
                        file.ContentType = BestEffortConvertImage(image, ms, file.ContentType);
                        var cacheItem = new TwImageCacheItem(ms.ToArray(), file.ContentType);
                        TwCache.Set(cacheKey, cacheItem);
                        return File(cacheItem.Bytes, cacheItem.ContentType);
                    }
                    else
                    {
                        return File(file.Data, file.ContentType);
                    }
                }
                else
                {
                    return NotFound(Localize("[{0}] was not found on the page [{1}].", fileNavigation, pageNavigation));
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to get image");
                throw;
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
        public async Task<ActionResult> Png(string givenPageNavigation, string givenFileNavigation, int? fileRevision = null)
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(givenPageNavigation, WikiPermission.Read);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                var pageNavigation = new TwNamespaceNavigation(givenPageNavigation);
                var fileNavigation = new TwNamespaceNavigation(givenFileNavigation);

                var scale = GetQueryValue<int?>("Scale");
                var maxWidth = GetQueryValue<int?>("MaxWidth");

                var cacheKey = TwCacheKeyFunction.Build(TwCache.Category.Page, [givenPageNavigation, givenFileNavigation, fileRevision, scale, maxWidth]);
                if (TwCache.TryGet<TwImageCacheItem>(cacheKey, out var cached))
                {
                    return File(cached.Bytes, cached.ContentType);
                }

                var file = await PageFileRepository.GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(pageNavigation.Canonical, fileNavigation.Canonical, fileRevision);
                if (file != null)
                {
                    var img = SixLabors.ImageSharp.Image.Load(new MemoryStream(Utility.Decompress(file.Data)));

                    if (scale > 500)
                    {
                        scale = 500;
                    }

                    //Enforce scale if specified.
                    if (scale != null && scale != 100)
                    {
                        int width = (int)(img.Width * (scale / 100.0));
                        int height = (int)(img.Height * (scale / 100.0));

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

                        using var image = TwImages.ResizeImage(img, width, height);
                        using var ms = new MemoryStream();
                        image.SaveAsPng(ms);

                        var cacheItem = new TwImageCacheItem(ms.ToArray(), "image/png");
                        TwCache.Set(cacheKey, cacheItem);
                        return File(cacheItem.Bytes, cacheItem.ContentType);
                    }
                    //Enforce max width if specified.
                    else if (maxWidth > 0 && img.Width > maxWidth)
                    {
                        double widthScale = (double)maxWidth / img.Width;

                        int width = Math.Max(1, (int)Math.Round(img.Width * widthScale));
                        int height = Math.Max(1, (int)Math.Round(img.Height * widthScale));

                        using var image = TwImages.ResizeImage(img, width, height);
                        using var ms = new MemoryStream();
                        image.SaveAsPng(ms);

                        var cacheItem = new TwImageCacheItem(ms.ToArray(), "image/png");
                        TwCache.Set(cacheKey, cacheItem);
                        return File(cacheItem.Bytes, cacheItem.ContentType);
                    }
                    else
                    {
                        using var ms = new MemoryStream();
                        img.SaveAsPng(ms);

                        var cacheItem = new TwImageCacheItem(ms.ToArray(), "image/png");
                        TwCache.Set(cacheKey, cacheItem);
                        return File(cacheItem.Bytes, cacheItem.ContentType);
                    }
                }
                else
                {
                    return NotFound(Localize("[{0}] was not found on the page [{1}].", fileNavigation, pageNavigation));
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to get PNG image");
                throw;
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
        public async Task<ActionResult> Binary(string givenPageNavigation, string givenFileNavigation, int? fileRevision = null)
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(givenPageNavigation, WikiPermission.Read);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                var pageNavigation = new TwNamespaceNavigation(givenPageNavigation);
                var fileNavigation = new TwNamespaceNavigation(givenFileNavigation);

                var file = await PageFileRepository.GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(pageNavigation.Canonical, fileNavigation.Canonical, fileRevision);

                if (file != null)
                {
                    return File(file.Data.ToArray(), file.ContentType);
                }
                else
                {
                    HttpContext.Response.StatusCode = 404;
                    return NotFound(Localize("[{0}] was not found on the page [{1}].", fileNavigation, pageNavigation));
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to get binary file");
                throw;
            }
        }

        /// <summary>
        /// Populate the upload page. Shows the attachments.
        /// </summary>
        [Authorize]
        [HttpGet("Revisions/{givenPageNavigation}/{givenFileNavigation}")]
        public async Task<ActionResult> Revisions(string givenPageNavigation, string givenFileNavigation)
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(givenPageNavigation, WikiPermission.Read);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                var pageNavigation = new TwNamespaceNavigation(givenPageNavigation);
                var fileNavigation = new TwNamespaceNavigation(givenFileNavigation);

                var model = new PageFileRevisionsViewModel()
                {
                    PageNavigation = pageNavigation.Canonical,
                    FileNavigation = fileNavigation.Canonical,
                    Revisions = await PageFileRepository.GetPageFileAttachmentRevisionsByPageAndFileNavigationPaged
                        (pageNavigation.Canonical, fileNavigation.Canonical, GetQueryValue("page", 1))
                };

                model.PaginationPageCount = (model.Revisions.FirstOrDefault()?.PaginationPageCount ?? 0);

                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to get page file revisions");
                throw;
            }
        }

        /// <summary>
        /// Populate the upload page. Shows the attachments.
        /// </summary>
        [Authorize]
        [HttpGet("PageAttachments/{givenPageNavigation}")]
        public async Task<ActionResult> PageAttachments(string givenPageNavigation)
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(givenPageNavigation, WikiPermission.Read);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                var pageNavigation = new TwNamespaceNavigation(givenPageNavigation);

                var page = await PageRepository.GetPageRevisionByNavigation(pageNavigation);
                if (page != null)
                {
                    var pageFiles = await PageFileRepository.GetPageFilesInfoByPageId(page.Id);

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
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to get page attachments");
                throw;
            }
        }

        /// <summary>
        /// Uploads a file by drag drop.
        /// </summary>
        [Authorize]
        [HttpPost("UploadDragDrop/{givenPageNavigation}")]
        public async Task<ActionResult> UploadDragDrop(string givenPageNavigation, List<IFormFile> postedFiles)
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(givenPageNavigation, [WikiPermission.Create, WikiPermission.Edit]);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                try
                {
                    var pageNavigation = new TwNamespaceNavigation(givenPageNavigation);

                    var page = (await PageRepository.GetPageInfoByNavigation(pageNavigation.Canonical)).EnsureNotNull();

                    foreach (IFormFile file in postedFiles)
                    {
                        if (file != null)
                        {
                            var fileSize = file.Length;
                            if (fileSize > 0)
                            {
                                if (fileSize > WikiConfiguration.MaxAttachmentFileSize)
                                {
                                    return Json(new { message = Localize("Could not attach file: [{0}], too large.", file.FileName) });
                                }

                                var fileName = HttpUtility.UrlDecode(file.FileName);

                                await PageFileRepository.UpsertPageFile(new TwPageFileAttachment()
                                {
                                    Data = Utility.ConvertHttpFileToBytes(file),
                                    CreatedDate = DateTime.UtcNow,
                                    PageId = page.Id,
                                    Name = fileName,
                                    FileNavigation = TwNavigation.Clean(fileName),
                                    Size = fileSize,
                                    ContentType = Utility.GetMimeType(fileName)
                                }, (SessionState.Profile?.UserId).EnsureNotNullOrEmpty());
                            }
                        }
                    }
                    return Json(new { success = true, message = Localize("files: {0:n0}", postedFiles.Count) });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to upload file");
                    return StatusCode(500, new { success = false, message = Localize("An error occurred: {0}", ex.Message) });
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to upload file via drag and drop");
                throw;
            }
        }

        /// <summary>
        /// Uploads a file by manually selecting it for upload.
        /// </summary>
        [Authorize]
        [HttpPost("ManualUpload/{givenPageNavigation}")]
        public async Task<ActionResult> ManualUpload(string givenPageNavigation, IFormFile fileData)
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(givenPageNavigation, [WikiPermission.Create, WikiPermission.Edit]);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                var pageNavigation = new TwNamespaceNavigation(givenPageNavigation);

                var page = (await PageRepository.GetPageInfoByNavigation(pageNavigation.Canonical)).EnsureNotNull();

                if (fileData != null)
                {
                    var fileSize = fileData.Length;
                    if (fileSize > 0)
                    {
                        if (fileSize > WikiConfiguration.MaxAttachmentFileSize)
                        {
                            return Content(Localize("Could not save the attached file, too large"));
                        }

                        var fileName = HttpUtility.UrlDecode(fileData.FileName);

                        await PageFileRepository.UpsertPageFile(new TwPageFileAttachment()
                        {
                            Data = Utility.ConvertHttpFileToBytes(fileData),
                            CreatedDate = DateTime.UtcNow,
                            PageId = page.Id,
                            Name = fileName,
                            FileNavigation = TwNavigation.Clean(fileName),
                            Size = fileSize,
                            ContentType = Utility.GetMimeType(fileName)
                        }, (SessionState.Profile?.UserId).EnsureNotNullOrEmpty());

                        return Content(Localize("Success"));
                    }
                }

                return Content(Localize("Failure"));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to manually upload file");
                throw;
            }
        }

        /// <summary>
        /// Allows a user to delete a page attachment from a page.
        /// </summary>
        [HttpPost("Detach/{givenPageNavigation}/{givenFileNavigation}/{pageRevision}")]
        public async Task<ActionResult> Detach(string givenPageNavigation, string givenFileNavigation, int pageRevision)
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(givenPageNavigation, WikiPermission.Delete);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }

                await PageFileRepository.DetachPageRevisionAttachment(
                    new TwNamespaceNavigation(givenPageNavigation).Canonical,
                    new TwNamespaceNavigation(givenFileNavigation).Canonical, pageRevision);

                return Content(Localize("Success"));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to detach page attachment");
                throw;
            }
        }

        #region AutoComplete.

        [Authorize]
        [HttpGet("AutoCompleteEmoji")]
        public async Task<ActionResult> AutoCompleteEmoji([FromQuery] string? q = null)
        {
            try
            {
                var emojis = await EmojiRepository.AutoCompleteEmoji(q ?? string.Empty);

                return Json(emojis.Select(o => new
                {
                    text = o
                }));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to autocomplete emoji");
                throw;
            }
        }

        #endregion

        /// <summary>
        /// Gets a file from the database, converts it to a PNG with optional scaling and returns it to the client.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("Emoji/{givenEmojiNavigation}")]
        public async Task<ActionResult> Emoji(string givenEmojiNavigation)
        {
            try
            {
                var emojiNavigation = TwNavigation.Clean(givenEmojiNavigation);

                if (string.IsNullOrEmpty(emojiNavigation) == false)
                {
                    var givenScale = GetQueryValue("Scale", 100);

                    string shortcut = $"%%{emojiNavigation.ToLowerInvariant()}%%";
                    var emoji = WikiConfiguration.Emojis.Where(o => o.Shortcut == shortcut).FirstOrDefault();
                    if (emoji != null)
                    {
                        //Do we have this scale cached already?
                        var scaledImageCacheKey = TwCacheKey.Build(TwCache.Category.Emoji, [shortcut, givenScale]);
                        if (TwCache.TryGet<TwImageCacheItem>(scaledImageCacheKey, out var cachedEmoji))
                        {
                            return File(cachedEmoji.Bytes, cachedEmoji.ContentType);
                        }

                        var imageCacheKey = TwCacheKey.Build(TwCache.Category.Emoji, [shortcut]);
                        emoji.ImageData = TwCache.Get<byte[]>(imageCacheKey);
                        if (emoji.ImageData == null)
                        {
                            //We don't get the bytes by default, that would be a lot of RAM for all the thousands of images.
                            emoji.ImageData = (await EmojiRepository.GetEmojiByName(emoji.Name))?.ImageData;

                            if (emoji.ImageData == null)
                            {
                                return NotFound(Localize("Emoji {0} was not found", emojiNavigation));
                            }

                            TwCache.Set(imageCacheKey, emoji.ImageData);
                        }

                        if (emoji.ImageData != null)
                        {
                            var decompressedImageBytes = Utility.Decompress(emoji.ImageData);

                            var img = SixLabors.ImageSharp.Image.Load(new MemoryStream(decompressedImageBytes));

                            if (givenScale > 500)
                            {
                                givenScale = 500;
                            }

                            var (Width, Height) = Utility.ScaleToMaxOf(img.Width, img.Height, WikiConfiguration.DefaultEmojiHeight);

                            //Adjust to any specified scaling.
                            Height = (int)(Height * (givenScale / 100.0));
                            Width = (int)(Width * (givenScale / 100.0));

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
                                var itemCache = new TwImageCacheItem(resized, "image/gif");
                                TwCache.Set(scaledImageCacheKey, itemCache);
                                return File(itemCache.Bytes, itemCache.ContentType);
                            }
                            else
                            {
                                using var image = TwImages.ResizeImage(img, Width, Height);
                                using var ms = new MemoryStream();
                                image.SaveAsPng(ms);

                                var itemCache = new TwImageCacheItem(ms.ToArray(), "image/png");
                                TwCache.Set(scaledImageCacheKey, itemCache);

                                return File(itemCache.Bytes, itemCache.ContentType);
                            }
                        }
                    }
                }

                return NotFound(Localize("Emoji {0} was not found", emojiNavigation));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to get emoji image");
                throw;
            }
        }
    }
}
