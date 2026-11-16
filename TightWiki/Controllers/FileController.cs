using ImageMagick;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NTDLS.Helpers;
using TightWiki.Exceptions;
using TightWiki.Library;
using TightWiki.Library.Caching;
using TightWiki.Library.Security;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;
using TightWiki.Repository;
using TightWiki.ViewModels.File;

using TwPermission = TightWiki.Plugin.TwPermission;

namespace TightWiki.Controllers
{
    [Route("File")]
    public class FileController(
            ILogger<ITwEngine> logger,
            ITwEmojiRepository emojiRepository,
            ITwPageRepository pageRepository,
            ITwSharedLocalizationText localizer,
            SignInManager<IdentityUser> signInManager,
            TwConfiguration wikiConfiguration,
            UserManager<IdentityUser> userManager,
            ITwDatabaseManager databaseManager,
            TwAttachmentUploads attachmentUploads
        )
        : TwController<FileController>(logger, signInManager, userManager, localizer, wikiConfiguration, databaseManager)
    {
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
                //Checked before the image cache so that a cached image is never served to someone who can not read the page.
                try
                {
                    await SessionState.RequirePermission(givenPageNavigation, TwPermission.Read);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }

                var pageNavigation = new TwNamespaceNavigation(givenPageNavigation);
                var fileNavigation = new TwNamespaceNavigation(givenFileNavigation);

                var scale = GetQueryValue<int?>("Scale");
                var maxWidth = GetQueryValue<int?>("MaxWidth");

                var cacheKey = MemCacheKeyFunction.Build(MemCache.Category.Page, [givenPageNavigation, givenFileNavigation, fileRevision, scale, maxWidth]);
                if (MemCache.TryGet<TwImageCacheItem>(cacheKey, out var cached))
                {
                    return File(cached.Bytes, cached.ContentType);
                }

                var file = await pageRepository.GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(pageNavigation.Canonical, fileNavigation.Canonical, fileRevision, loadContent: false);
                if (file != null)
                {
                    if (!CanResizeImage(file))
                    {
                        //Icons, vector images, very large images and anything that is not an image are sent as they are.
                        return AttachmentContent(file);
                    }

                    var data = await ReadAttachmentContent(file);
                    var img = new MagickImage(data);

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
                            var resized = ImagesUtility.ResizeGifImage(data, width, height);
                            return File(resized, "image/gif");
                        }
                        else
                        {
                            using var image = ImagesUtility.ResizeImage(img, (uint)width, (uint)height);
                            using var ms = new MemoryStream();
                            var resizedContentType = ImagesUtility.BestEffortConvertImage(image, ms, file.ContentType);
                            var cacheItem = new TwImageCacheItem(ms.ToArray(), resizedContentType);
                            MemCache.Set(cacheKey, cacheItem);
                            return File(cacheItem.Bytes, cacheItem.ContentType);
                        }
                    }
                    //Enforce max width if specified.
                    else if (maxWidth > 0 && img.Width > maxWidth)
                    {
                        double widthScale = (double)maxWidth / img.Width;

                        int width = Math.Max(1, (int)Math.Round(img.Width * widthScale));
                        int height = Math.Max(1, (int)Math.Round(img.Height * widthScale));

                        using var image = ImagesUtility.ResizeImage(img, (uint)width, (uint)height);
                        using var ms = new MemoryStream();
                        var resizedContentType = ImagesUtility.BestEffortConvertImage(image, ms, file.ContentType);
                        var cacheItem = new TwImageCacheItem(ms.ToArray(), resizedContentType);
                        MemCache.Set(cacheKey, cacheItem);
                        return File(cacheItem.Bytes, cacheItem.ContentType);
                    }
                    else
                    {
                        return AttachmentContent(file);
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
                    await SessionState.RequirePermission(givenPageNavigation, TwPermission.Read);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                var pageNavigation = new TwNamespaceNavigation(givenPageNavigation);
                var fileNavigation = new TwNamespaceNavigation(givenFileNavigation);

                var scale = GetQueryValue<int?>("Scale");
                var maxWidth = GetQueryValue<int?>("MaxWidth");

                var cacheKey = MemCacheKeyFunction.Build(MemCache.Category.Page, [givenPageNavigation, givenFileNavigation, fileRevision, scale, maxWidth]);
                if (MemCache.TryGet<TwImageCacheItem>(cacheKey, out var cached))
                {
                    return File(cached.Bytes, cached.ContentType);
                }

                var file = await pageRepository.GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(pageNavigation.Canonical, fileNavigation.Canonical, fileRevision, loadContent: false);
                if (file != null)
                {
                    if (!CanResizeImage(file))
                    {
                        return AttachmentContent(file);
                    }

                    //Attachments are stored as they were uploaded (only emoji are stored compressed).
                    var img = new MagickImage(await ReadAttachmentContent(file));

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

                        using var image = ImagesUtility.ResizeImage(img, (uint)width, (uint)height);
                        using var ms = new MemoryStream();
                        image.Write(ms, MagickFormat.Png);

                        var cacheItem = new TwImageCacheItem(ms.ToArray(), "image/png");
                        MemCache.Set(cacheKey, cacheItem);
                        return File(cacheItem.Bytes, cacheItem.ContentType);
                    }
                    //Enforce max width if specified.
                    else if (maxWidth > 0 && img.Width > maxWidth)
                    {
                        double widthScale = (double)maxWidth / img.Width;

                        int width = Math.Max(1, (int)Math.Round(img.Width * widthScale));
                        int height = Math.Max(1, (int)Math.Round(img.Height * widthScale));

                        using var image = ImagesUtility.ResizeImage(img, (uint)width, (uint)height);
                        using var ms = new MemoryStream();
                        image.Write(ms, MagickFormat.Png);

                        var cacheItem = new TwImageCacheItem(ms.ToArray(), "image/png");
                        MemCache.Set(cacheKey, cacheItem);
                        return File(cacheItem.Bytes, cacheItem.ContentType);
                    }
                    else
                    {
                        using var ms = new MemoryStream();
                        img.Write(ms, MagickFormat.Png);

                        var cacheItem = new TwImageCacheItem(ms.ToArray(), "image/png");
                        MemCache.Set(cacheKey, cacheItem);
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
                    await SessionState.RequirePermission(givenPageNavigation, TwPermission.Read);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                var pageNavigation = new TwNamespaceNavigation(givenPageNavigation);
                var fileNavigation = new TwNamespaceNavigation(givenFileNavigation);

                var file = await pageRepository.GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(pageNavigation.Canonical, fileNavigation.Canonical, fileRevision, loadContent: false);

                if (file != null)
                {
                    return AttachmentContent(file);
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
                    await SessionState.RequirePermission(givenPageNavigation, TwPermission.Read);
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
                    Revisions = await pageRepository.GetPageFileAttachmentRevisionsByPageAndFileNavigationPaged
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
                    await SessionState.RequirePermission(givenPageNavigation, TwPermission.Read);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                var pageNavigation = new TwNamespaceNavigation(givenPageNavigation);

                var page = await pageRepository.GetPageRevisionByNavigation(pageNavigation);
                if (page != null)
                {
                    var pageFiles = await pageRepository.GetPageFilesInfoByPageId(page.Id);

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

        #region Uploads.

        public class StartUploadRequest
        {
            public string FileName { get; set; } = string.Empty;
            public long Size { get; set; }
        }

        /// <summary>
        /// Starts an attachment upload. The content is then sent in pieces with UploadPiece and attached with CompleteUpload.
        /// </summary>
        [Authorize]
        [HttpPost("Upload/Start/{givenPageNavigation}")]
        public async Task<ActionResult> StartUpload(string givenPageNavigation, [FromBody] StartUploadRequest request)
        {
            try
            {
                await SessionState.RequirePermission(givenPageNavigation, [TwPermission.Create, TwPermission.Edit]);

                var fileName = Path.GetFileName(request.FileName?.Trim() ?? string.Empty);
                if (string.IsNullOrEmpty(fileName))
                {
                    return UploadError(StatusCodes.Status400BadRequest, Localize("The file has no name."));
                }
                if (request.Size <= 0)
                {
                    return UploadError(StatusCodes.Status400BadRequest, Localize("Could not attach file: [{0}], it is empty.", fileName));
                }
                if (request.Size > WikiConfiguration.MaxAttachmentFileSize)
                {
                    return UploadError(StatusCodes.Status413PayloadTooLarge, Localize("Could not attach file: [{0}], it is larger than the {1} allowed.",
                        fileName, NTDLS.Helpers.Formatters.FileSize(WikiConfiguration.MaxAttachmentFileSize)));
                }

                var freeSpace = GetFreeSpace(attachmentUploads.Storage.RootPath);
                if (freeSpace != null && freeSpace < request.Size + MinimumFreeSpaceAfterUpload)
                {
                    return UploadError(StatusCodes.Status507InsufficientStorage, Localize("Could not attach file: [{0}], there is not enough free disk space on the server.", fileName));
                }

                var pageNavigation = new TwNamespaceNavigation(givenPageNavigation);
                var page = await pageRepository.GetPageInfoByNavigation(pageNavigation.Canonical);
                if (page == null)
                {
                    return UploadError(StatusCodes.Status404NotFound, Localize("The page was not found, save the page before uploading files."));
                }

                var upload = attachmentUploads.Start(CurrentUserId, page.Id, pageNavigation.Canonical, fileName, request.Size);

                return Json(new { uploadId = upload.UploadId, maxPieceSize = TwAttachmentUploads.MaxPieceSize, received = 0L });
            }
            catch (Exception ex)
            {
                return UploadError(ex, "Failed to start an attachment upload.");
            }
        }

        /// <summary>
        /// Returns how much of an upload the server has, so an interrupted upload can continue from there.
        /// </summary>
        [Authorize]
        [HttpGet("Upload/{uploadId}")]
        public ActionResult UploadStatus(string uploadId)
        {
            try
            {
                var upload = attachmentUploads.Get(uploadId, CurrentUserId);
                return Json(new { uploadId = upload.UploadId, size = upload.Size, received = attachmentUploads.GetReceived(uploadId) });
            }
            catch (Exception ex)
            {
                return UploadError(ex, "Failed to get the status of an attachment upload.");
            }
        }

        /// <summary>
        /// Receives one piece of an upload as the raw request body, at the given offset.
        /// </summary>
        [Authorize]
        [HttpPut("Upload/{uploadId}/{offset:long}")]
        [RequestSizeLimit(TwAttachmentUploads.MaxPieceSize + 1024 * 1024)]
        public async Task<ActionResult> UploadPiece(string uploadId, long offset)
        {
            try
            {
                var upload = attachmentUploads.Get(uploadId, CurrentUserId);
                var received = await attachmentUploads.AppendAsync(upload, offset, Request.Body, HttpContext.RequestAborted);
                return Json(new { received });
            }
            catch (Exception ex)
            {
                return UploadError(ex, "Failed to receive a piece of an attachment upload.");
            }
        }

        /// <summary>
        /// Attaches a completely received upload to the page. A new file revision is only created if the content changed.
        /// </summary>
        [Authorize]
        [HttpPost("Upload/{uploadId}/Complete")]
        public async Task<ActionResult> CompleteUpload(string uploadId)
        {
            try
            {
                var upload = attachmentUploads.Get(uploadId, CurrentUserId);

                //Permissions may have changed while the file was being uploaded.
                await SessionState.RequirePermission(upload.PageNavigation, [TwPermission.Create, TwPermission.Edit]);

                return await attachmentUploads.WithLock<ActionResult>(uploadId, async () =>
                {
                    var partPath = attachmentUploads.GetPartPath(uploadId);
                    var received = attachmentUploads.GetReceived(uploadId);
                    if (received != upload.Size)
                    {
                        return UploadError(StatusCodes.Status409Conflict, Localize("The upload is not complete."), received);
                    }

                    int dataHash;
                    await using (var stream = new FileStream(partPath, FileMode.Open, FileAccess.Read, FileShare.Read,
                        1024 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan))
                    {
                        dataHash = await SecurityUtility.Crc32Async(stream, HttpContext.RequestAborted);
                    }

                    var changed = await pageRepository.UpsertPageFileFromIncoming(new TwPageFileAttachment()
                    {
                        CreatedDate = DateTime.UtcNow,
                        PageId = upload.PageId,
                        Name = upload.FileName,
                        FileNavigation = TwNavigation.Clean(upload.FileName),
                        Size = upload.Size,
                        ContentType = Utility.GetMimeType(upload.FileName)
                    }, partPath, dataHash, CurrentUserId);

                    attachmentUploads.Delete(uploadId);

                    return Json(new { success = true, fileName = upload.FileName, changed });
                });
            }
            catch (Exception ex)
            {
                return UploadError(ex, "Failed to complete an attachment upload.");
            }
        }

        /// <summary>
        /// Cancels an upload and deletes what was received.
        /// </summary>
        [Authorize]
        [HttpDelete("Upload/{uploadId}")]
        public async Task<ActionResult> CancelUpload(string uploadId)
        {
            try
            {
                attachmentUploads.Get(uploadId, CurrentUserId);
                return await attachmentUploads.WithLock<ActionResult>(uploadId, () =>
                {
                    attachmentUploads.Delete(uploadId);
                    return Task.FromResult<ActionResult>(Json(new { success = true }));
                });
            }
            catch (Exception ex)
            {
                return UploadError(ex, "Failed to cancel an attachment upload.");
            }
        }

        /// <summary>
        /// Uploads must leave this much disk space free, so a large upload can not fill the disk the databases are on.
        /// </summary>
        private const long MinimumFreeSpaceAfterUpload = 512L * 1024 * 1024;

        private Guid CurrentUserId => (SessionState.Profile?.UserId).EnsureNotNullOrEmpty();

        private static long? GetFreeSpace(string path)
        {
            try
            {
                var root = Path.GetPathRoot(Path.GetFullPath(path));
                return string.IsNullOrEmpty(root) ? null : new DriveInfo(root).AvailableFreeSpace;
            }
            catch
            {
                return null; //Not a drive that can be queried, such as a network share.
            }
        }

        private JsonResult UploadError(int statusCode, string message, long? received = null)
        {
            Response.StatusCode = statusCode;
            return Json(new { success = false, message, received });
        }

        private JsonResult UploadError(Exception ex, string logMessage)
        {
            switch (ex.GetBaseException())
            {
                case TwAttachmentUploadException uploadException:
                    return UploadError(uploadException.StatusCode, Localize(uploadException.Message), uploadException.Received);
                case UnauthorizedException:
                    return UploadError(StatusCodes.Status403Forbidden, ex.GetBaseException().Message);
                case OperationCanceledException:
                    return UploadError(499, Localize("The upload was interrupted."));
                default:
                    Logger.LogError(ex, logMessage);
                    return UploadError(StatusCodes.Status500InternalServerError, Localize("An error occurred: {0}", ex.GetBaseException().Message));
            }
        }

        #endregion

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
                    await SessionState.RequirePermission(givenPageNavigation, TwPermission.Delete);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }

                await pageRepository.DetachPageRevisionAttachment(
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
                var emojis = await emojiRepository.AutoCompleteEmoji(q ?? string.Empty);

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
                        var scaledImageCacheKey = MemCacheKey.Build(MemCache.Category.Emoji, [shortcut, givenScale]);
                        if (MemCache.TryGet<TwImageCacheItem>(scaledImageCacheKey, out var cachedEmoji))
                        {
                            return File(cachedEmoji.Bytes, cachedEmoji.ContentType);
                        }

                        var imageCacheKey = MemCacheKey.Build(MemCache.Category.Emoji, [shortcut]);
                        emoji.ImageData = MemCache.Get<byte[]>(imageCacheKey);
                        if (emoji.ImageData == null)
                        {
                            //We don't get the bytes by default, that would be a lot of RAM for all the thousands of images.
                            emoji.ImageData = (await emojiRepository.GetEmojiByName(emoji.Name))?.ImageData;

                            if (emoji.ImageData == null)
                            {
                                return NotFound(Localize("Emoji {0} was not found", emojiNavigation));
                            }

                            MemCache.Set(imageCacheKey, emoji.ImageData);
                        }

                        if (emoji.ImageData != null)
                        {
                            var decompressedImageBytes = Utility.Decompress(emoji.ImageData);

                            var img = new MagickImage(decompressedImageBytes);

                            if (givenScale > 500)
                            {
                                givenScale = 500;
                            }

                            var (Width, Height) = ImagesUtility.ScaleToMaxOf((int)img.Width, (int)img.Height, WikiConfiguration.DefaultEmojiHeight);

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
                                var resized = ImagesUtility.ResizeGifImage(decompressedImageBytes, Width, Height);
                                var itemCache = new TwImageCacheItem(resized, "image/gif");
                                MemCache.Set(scaledImageCacheKey, itemCache);
                                return File(itemCache.Bytes, itemCache.ContentType);
                            }
                            else
                            {
                                using var image = ImagesUtility.ResizeImage(img, (uint)Width, (uint)Height);
                                using var ms = new MemoryStream();
                                image.Write(ms, MagickFormat.Png);

                                var itemCache = new TwImageCacheItem(ms.ToArray(), "image/png");
                                MemCache.Set(scaledImageCacheKey, itemCache);

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
