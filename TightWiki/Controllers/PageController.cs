using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NTDLS.Helpers;
using SixLabors.ImageSharp;
using System.Text;
using TightWiki.Caching;
using TightWiki.Configuration;
using TightWiki.Engine;
using TightWiki.Engine.Implementation.Utility;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.Library;
using TightWiki.Models.DataModels;
using TightWiki.Models.ViewModels.Page;
using TightWiki.Repository;
using static TightWiki.Library.Constants;
using static TightWiki.Library.Images;

namespace TightWiki.Controllers
{
    [Route("")]
    public class PageController(ITightEngine tightEngine, SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        : WikiControllerBase(signInManager, userManager)
    {
        [AllowAnonymous]
        [Route("/robots.txt")]
        public ContentResult RobotsTxt()
        {
            var sb = new StringBuilder();
            sb.AppendLine("User-agent: *")
                .AppendLine("Allow: /");

            return Content(sb.ToString(), "text/plain", Encoding.UTF8);
        }

        #region Display.

        /// <summary>
        /// Default controller for root requests. e.g. http://127.0.0.1/
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Display()
            => Display("home", null);

        [HttpGet("{givenCanonical}/{pageRevision:int?}")]
        public IActionResult Display(string givenCanonical, int? pageRevision)
        {
            SessionState.RequireViewPermission();

            var model = new PageDisplayViewModel();
            var navigation = new NamespaceNavigation(givenCanonical);

            var page = PageRepository.GetPageRevisionByNavigation(navigation.Canonical, pageRevision);
            if (page != null)
            {
                var instructions = PageRepository.GetPageProcessingInstructionsByPageId(page.Id);
                model.Revision = page.Revision;
                model.MostCurrentRevision = page.MostCurrentRevision;
                model.Name = page.Name;
                model.Namespace = page.Namespace;
                model.Navigation = page.Navigation;
                model.HideFooterComments = instructions.Contains(WikiInstruction.HideFooterComments);
                model.HideFooterLastModified = instructions.Contains(WikiInstruction.HideFooterLastModified);
                model.ModifiedByUserName = page.ModifiedByUserName;
                model.ModifiedDate = SessionState.LocalizeDateTime(page.ModifiedDate);

                SessionState.SetPageId(page.Id, pageRevision);

                if (GlobalConfiguration.PageCacheSeconds > 0)
                {
                    string queryKey = string.Empty;
                    foreach (var query in Request.Query)
                    {
                        queryKey += $"{query.Key}:{query.Value}";
                    }

                    var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Page, [page.Navigation, page.Revision, queryKey]);
                    if (WikiCache.TryGet<string>(cacheKey, out var result))
                    {
                        model.Body = result;
                        WikiCache.Put(cacheKey, result); //Update the cache expiration.
                    }
                    else
                    {
                        var state = tightEngine.Transform(SessionState, page, pageRevision);

                        model.Body = state.HtmlResult;
                        if (state.ProcessingInstructions.Contains(WikiInstruction.NoCache) == false)
                        {
                            WikiCache.Put(cacheKey, state.HtmlResult); //This is cleared with the call to Cache.ClearCategory($"Page:{page.Navigation}");
                        }
                    }
                }
                else
                {
                    var state = tightEngine.Transform(SessionState, page, pageRevision);

                    model.Body = state.HtmlResult;
                }

                if (GlobalConfiguration.EnablePageComments && GlobalConfiguration.ShowCommentsOnPageFooter && model.HideFooterComments == false)
                {
                    var comments = PageRepository.GetPageCommentsPaged(navigation.Canonical, 1);

                    foreach (var comment in comments)
                    {
                        model.Comments.Add(new PageComment
                        {
                            PaginationPageCount = comment.PaginationPageCount,
                            UserNavigation = comment.UserNavigation,
                            Id = comment.Id,
                            UserName = comment.UserName,
                            UserId = comment.UserId,
                            Body = WikifierLite.Process(comment.Body),
                            CreatedDate = SessionState.LocalizeDateTime(comment.CreatedDate)
                        });
                    }
                }
            }
            else if (pageRevision != null)
            {
                var notExistPageName = ConfigurationRepository.Get<string>("Customization", "Revision Does Not Exists Page");
                string notExistPageNavigation = NamespaceNavigation.CleanAndValidate(notExistPageName);
                var notExistsPage = PageRepository.GetPageRevisionByNavigation(notExistPageNavigation).EnsureNotNull();

                SessionState.SetPageId(null, pageRevision);

                var state = tightEngine.Transform(SessionState, notExistsPage);

                SessionState.Page.Name = notExistsPage.Name;
                model.Body = state.HtmlResult;

                model.HideFooterComments = true;

                if (SessionState.IsAuthenticated && SessionState.CanCreate)
                {
                    SessionState.ShouldCreatePage = false;
                }
            }
            else
            {
                var notExistPageName = ConfigurationRepository.Get<string>("Customization", "Page Not Exists Page");
                string notExistPageNavigation = NamespaceNavigation.CleanAndValidate(notExistPageName);
                var notExistsPage = PageRepository.GetPageRevisionByNavigation(notExistPageNavigation).EnsureNotNull();

                SessionState.SetPageId(null, null);

                var state = tightEngine.Transform(SessionState, notExistsPage);

                SessionState.Page.Name = notExistsPage.Name;
                model.Body = state.HtmlResult;

                model.HideFooterComments = true;

                if (SessionState.IsAuthenticated && SessionState.CanCreate)
                {
                    SessionState.ShouldCreatePage = true;
                }
            }

            return View(model);
        }

        #endregion

        #region Search.

        [AllowAnonymous]
        [HttpGet("Page/Search")]
        public ActionResult Search()
        {
            string searchString = GetQueryValue("SearchString") ?? string.Empty;
            if (string.IsNullOrEmpty(searchString) == false)
            {
                var model = new PageSearchViewModel()
                {
                    Pages = PageRepository.PageSearchPaged(Utility.SplitToTokens(searchString), GetQueryValue("page", 1)),
                    SearchString = searchString
                };

                model.PaginationPageCount = (model.Pages.FirstOrDefault()?.PaginationPageCount ?? 0);

                return View(model);
            }

            return View(new PageSearchViewModel()
            {
                Pages = new List<Page>(),
                SearchString = searchString
            });
        }

        [AllowAnonymous]
        [HttpPost("Page/Search")]
        public ActionResult Search(PageSearchViewModel model)
        {
            string searchString = GetQueryValue("SearchString") ?? string.Empty;
            if (string.IsNullOrEmpty(searchString) == false)
            {
                model = new PageSearchViewModel()
                {
                    Pages = PageRepository.PageSearchPaged(Utility.SplitToTokens(searchString), GetQueryValue("page", 1)),
                    SearchString = searchString
                };

                model.PaginationPageCount = (model.Pages.FirstOrDefault()?.PaginationPageCount ?? 0);

                return View(model);
            }

            return View(new PageSearchViewModel()
            {
                Pages = new List<Page>(),
                SearchString = searchString
            });
        }

        #endregion

        #region Comments.

        [AllowAnonymous]
        [HttpGet("{givenCanonical}/Comments")]
        public ActionResult Comments(string givenCanonical)
        {
            SessionState.RequireViewPermission();

            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);

            var pageInfo = PageRepository.GetPageInfoByNavigation(pageNavigation);
            if (pageInfo == null)
            {
                return NotFound();
            }

            var deleteAction = GetQueryValue("Delete");
            if (string.IsNullOrEmpty(deleteAction) == false && SessionState.IsAuthenticated)
            {
                if (SessionState.CanModerate)
                {
                    //Moderators and administrators can delete comments that they do not own.
                    PageRepository.DeletePageCommentById(pageInfo.Id, int.Parse(deleteAction));
                }
                else
                {
                    PageRepository.DeletePageCommentByUserAndId(pageInfo.Id, SessionState.Profile.EnsureNotNull().UserId, int.Parse(deleteAction));
                }
            }

            var model = new PageCommentsViewModel();

            var comments = PageRepository.GetPageCommentsPaged(pageNavigation, GetQueryValue("page", 1));
            foreach (var comment in comments)
            {
                model.Comments.Add(new PageComment
                {
                    PaginationPageCount = comment.PaginationPageCount,
                    UserNavigation = comment.UserNavigation,
                    Id = comment.Id,
                    UserName = comment.UserName,
                    UserId = comment.UserId,
                    Body = WikifierLite.Process(comment.Body),
                    CreatedDate = SessionState.LocalizeDateTime(comment.CreatedDate)
                });
            }

            model.PaginationPageCount = (model.Comments.FirstOrDefault()?.PaginationPageCount ?? 0);

            SessionState.SetPageId(pageInfo.Id);

            return View(model);
        }

        /// <summary>
        /// Insert new page comment.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="givenCanonical"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("{givenCanonical}/Comments")]
        public ActionResult Comments(PageCommentsViewModel model, string givenCanonical)
        {
            SessionState.RequireEditPermission();

            if (!model.ValidateModelAndSetErrors(ModelState))
            {
                return View(model);
            }

            string? errorMessage = null;

            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);

            var pageInfo = PageRepository.GetPageInfoByNavigation(pageNavigation);
            if (pageInfo == null)
            {
                return NotFound();
            }

            PageRepository.InsertPageComment(pageInfo.Id, SessionState.Profile.EnsureNotNull().UserId, model.Comment);

            model = new PageCommentsViewModel()
            {
                ErrorMessage = errorMessage.DefaultWhenNull(string.Empty)
            };

            var comments = PageRepository.GetPageCommentsPaged(pageNavigation, GetQueryValue("page", 1));
            foreach (var comment in comments)
            {
                model.Comments.Add(new PageComment
                {
                    PaginationPageCount = comment.PaginationPageCount,
                    UserNavigation = comment.UserNavigation,
                    Id = comment.Id,
                    UserName = comment.UserName,
                    UserId = comment.UserId,
                    Body = WikifierLite.Process(comment.Body),
                    CreatedDate = SessionState.LocalizeDateTime(comment.CreatedDate)
                });
            }

            model.PaginationPageCount = (model.Comments.FirstOrDefault()?.PaginationPageCount ?? 0);

            SessionState.SetPageId(pageInfo.Id);

            return View(model);
        }

        #endregion

        #region Refresh.

        [Authorize]
        [HttpGet("{givenCanonical}/Refresh")]
        public ActionResult Refresh(string givenCanonical)
        {
            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);

            var page = PageRepository.GetPageRevisionByNavigation(pageNavigation, null, false);

            if (page != null)
            {
                Engine.Implementation.Helpers.RefreshPageMetadata(tightEngine, page, SessionState);
            }

            return Redirect($"/{pageNavigation}");
        }

        #endregion

        #region Revisions.

        [Authorize]
        [HttpGet("{givenCanonical}/Revisions")]
        public ActionResult Revisions(string givenCanonical)
        {
            SessionState.RequireViewPermission();

            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);

            var pageNumber = GetQueryValue("page", 1);
            var orderBy = GetQueryValue("OrderBy");
            var orderByDirection = GetQueryValue("OrderByDirection");

            var model = new RevisionsViewModel()
            {
                Revisions = PageRepository.GetPageRevisionsInfoByNavigationPaged(pageNavigation, pageNumber, orderBy, orderByDirection)
            };

            model.PaginationPageCount = (model.Revisions.FirstOrDefault()?.PaginationPageCount ?? 0);

            model.Revisions.ForEach(o =>
            {
                o.CreatedDate = SessionState.LocalizeDateTime(o.CreatedDate);
                o.ModifiedDate = SessionState.LocalizeDateTime(o.ModifiedDate);
            });

            foreach (var p in model.Revisions)
            {
                var thisRev = PageRepository.GetPageRevisionByNavigation(p.Navigation, p.Revision);
                var prevRev = PageRepository.GetPageRevisionByNavigation(p.Navigation, p.Revision - 1);
                p.ChangeSummary = Differentiator.GetComparisonSummary(thisRev?.Body ?? "", prevRev?.Body ?? "");
            }

            if (model.Revisions != null && model.Revisions.Count > 0)
            {
                SessionState.SetPageId(model.Revisions.First().PageId);
            }

            return View(model);
        }

        #endregion

        #region Delete.

        [Authorize]
        [HttpPost("{givenCanonical}/Delete")]
        public ActionResult Delete(string givenCanonical, PageDeleteViewModel model)
        {
            SessionState.RequireDeletePermission();

            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);

            var page = PageRepository.GetPageRevisionByNavigation(pageNavigation);
            var instructions = PageRepository.GetPageProcessingInstructionsByPageId(page.EnsureNotNull().Id);
            if (instructions.Contains(WikiInstruction.Protect))
            {
                model.ErrorMessage = "The page is protected and cannot be deleted. A moderator or an administrator must remove the protection before deletion.";
                return View(model);
            }

            bool confirmAction = bool.Parse(GetFormValue("IsActionConfirmed").EnsureNotNull());
            if (confirmAction == true && page != null)
            {
                PageRepository.MovePageToDeletedById(page.Id, (SessionState.Profile?.UserId).EnsureNotNullOrEmpty());
                WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.Page, [page.Navigation]));
                WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.Page, [page.Id]));
                return NotifyOfSuccess("The page has been deleted.", $"/Home");
            }

            return Redirect($"/{pageNavigation}");
        }

        [Authorize]
        [HttpGet("{givenCanonical}/Delete")]
        public ActionResult Delete(string givenCanonical)
        {
            SessionState.RequireDeletePermission();

            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);

            var page = PageRepository.GetPageRevisionByNavigation(pageNavigation).EnsureNotNull();

            var model = new PageDeleteViewModel()
            {
                CountOfAttachments = PageRepository.GetCountOfPageAttachmentsById(page.Id),
                PageName = page.Name,
                MostCurrentRevision = page.Revision,
                PageRevision = page.Revision
            };

            SessionState.SetPageId(page.Id);

            var instructions = PageRepository.GetPageProcessingInstructionsByPageId(page.Id);
            if (instructions.Contains(WikiInstruction.Protect))
            {
                return NotifyOfError("The page is protected and cannot be deleted. A moderator or an administrator must remove the protection before deletion.");
            }

            return View(model);
        }

        #endregion

        #region Revert.

        [Authorize]
        [HttpPost("{givenCanonical}/Revert/{pageRevision:int}")]
        public ActionResult Revert(string givenCanonical, int pageRevision, PageRevertViewModel model)
        {
            SessionState.RequireModeratePermission();

            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);

            bool confirmAction = bool.Parse(GetFormValue("IsActionConfirmed").EnsureNotNullOrEmpty());
            if (confirmAction == true)
            {
                var page = PageRepository.GetPageRevisionByNavigation(pageNavigation, pageRevision).EnsureNotNull();
                Engine.Implementation.Helpers.UpsertPage(tightEngine, page, SessionState);
                return NotifyOfSuccess("The page has been reverted.", $"/{pageNavigation}");
            }

            return Redirect($"/{pageNavigation}");
        }

        [Authorize]
        [HttpGet("{givenCanonical}/Revert/{pageRevision:int}")]
        public ActionResult Revert(string givenCanonical, int pageRevision)
        {
            SessionState.RequireModeratePermission();

            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);

            var mostCurrentPage = PageRepository.GetPageRevisionByNavigation(pageNavigation).EnsureNotNull();
            mostCurrentPage.CreatedDate = SessionState.LocalizeDateTime(mostCurrentPage.CreatedDate);
            mostCurrentPage.ModifiedDate = SessionState.LocalizeDateTime(mostCurrentPage.ModifiedDate);

            var revisionPage = PageRepository.GetPageRevisionByNavigation(pageNavigation, pageRevision).EnsureNotNull();
            revisionPage.CreatedDate = SessionState.LocalizeDateTime(revisionPage.CreatedDate);
            revisionPage.ModifiedDate = SessionState.LocalizeDateTime(revisionPage.ModifiedDate);

            var model = new PageRevertViewModel()
            {
                PageName = revisionPage.Name,
                HighestRevision = mostCurrentPage.Revision,
                HigherRevisionCount = revisionPage.HigherRevisionCount,
            };

            if (revisionPage != null)
            {
                SessionState.SetPageId(revisionPage.Id, pageRevision);
            }

            return View(model);
        }

        #endregion

        #region Edit.

        [Authorize]
        [HttpGet("{givenCanonical}/Edit")]
        [HttpGet("Page/Create")]
        public ActionResult Edit(string givenCanonical)
        {
            SessionState.RequireEditPermission();

            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);

            var page = PageRepository.GetPageRevisionByNavigation(pageNavigation);
            if (page != null)
            {
                var instructions = PageRepository.GetPageProcessingInstructionsByPageId(page.EnsureNotNull().Id);
                if (SessionState.CanModerate == false && instructions.Contains(WikiInstruction.Protect))
                {
                    return NotifyOfError("The page is protected and cannot be modified except by a moderator or an administrator unless the protection is removed.");
                }

                SessionState.SetPageId(page.Id);

                return View(new PageEditViewModel()
                {
                    Id = page.Id,
                    Body = page.Body,
                    Name = page.Name,
                    Navigation = NamespaceNavigation.CleanAndValidate(page.Navigation),
                    Description = page.Description
                });
            }
            else
            {
                var pageName = GetQueryValue("Name").DefaultWhenNullOrEmpty(pageNavigation);

                string templateName = ConfigurationRepository.Get<string>("Customization", "New Page Template").EnsureNotNull();
                string templateNavigation = NamespaceNavigation.CleanAndValidate(templateName);
                var templatePage = PageRepository.GetPageRevisionByNavigation(templateNavigation);

                if (templatePage == null)
                {
                    templatePage = new Page();
                }

                return View(new PageEditViewModel()
                {
                    Body = templatePage.Body,
                    Name = pageName?.Replace('_', ' ') ?? string.Empty,
                    Navigation = NamespaceNavigation.CleanAndValidate(pageNavigation)
                });
            }
        }

        [Authorize]
        [HttpPost("{givenCanonical}/Edit")]
        [HttpPost("Page/Create")]
        public ActionResult Edit(PageEditViewModel model)
        {
            SessionState.RequireEditPermission();

            if (!model.ValidateModelAndSetErrors(ModelState))
            {
                return View(model);
            }

            if (model.Id == 0) //Saving a new page.
            {
                var page = new Page()
                {
                    CreatedDate = DateTime.UtcNow,
                    CreatedByUserId = SessionState.Profile.EnsureNotNull().UserId,
                    ModifiedDate = DateTime.UtcNow,
                    ModifiedByUserId = SessionState.Profile.UserId,
                    Body = model.Body ?? "",
                    Name = model.Name,
                    Navigation = NamespaceNavigation.CleanAndValidate(model.Name),
                    Description = model.Description ?? ""
                };

                if (PageRepository.GetPageInfoByNavigation(page.Navigation) != null)
                {
                    model.ErrorMessage = "The page name you entered already exists.";
                    return View(model);
                }

                page.Id = Engine.Implementation.Helpers.UpsertPage(tightEngine, page, SessionState);

                SessionState.SetPageId(page.Id);

                return NotifyOfSuccess("The page has been created.", $"/{page.Navigation}/Edit");
            }
            else
            {
                var page = PageRepository.GetPageRevisionById(model.Id).EnsureNotNull();
                var instructions = PageRepository.GetPageProcessingInstructionsByPageId(page.Id);
                if (SessionState.CanModerate == false && instructions.Contains(WikiInstruction.Protect))
                {
                    return NotifyOfError("The page is protected and cannot be modified except by a moderator or an administrator unless the protection is removed.");
                }

                string originalNavigation = string.Empty;

                model.Navigation = NamespaceNavigation.CleanAndValidate(model.Name);

                if (page.Navigation.ToLower() != model.Navigation.ToLower())
                {
                    if (PageRepository.GetPageInfoByNavigation(model.Navigation) != null)
                    {
                        model.ErrorMessage = "The page name you entered already exists.";
                        return View(model);
                    }

                    originalNavigation = page.Navigation; //So we can clear cache and this also indicates that we need to redirect to the new name.
                }

                page.ModifiedDate = DateTime.UtcNow;
                page.ModifiedByUserId = SessionState.Profile.EnsureNotNull().UserId;
                page.Body = model.Body ?? "";
                page.Name = model.Name;
                page.Navigation = NamespaceNavigation.CleanAndValidate(model.Name);
                page.Description = model.Description ?? "";

                Engine.Implementation.Helpers.UpsertPage(tightEngine, page, SessionState);

                SessionState.SetPageId(page.Id);

                model.SuccessMessage = "The page was saved.";

                if (string.IsNullOrWhiteSpace(originalNavigation) == false)
                {
                    WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.Page, [originalNavigation]));
                    WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.Page, [page.Id]));
                    return Redirect($"/{page.Navigation}/Edit");
                }

                return View(model);
            }
        }

        #endregion

        #region File.

        /// <summary>
        /// Gets an image attached to a page.
        /// </summary>
        /// <param name="givenPageNavigation">The navigation link of the page.</param>
        /// <param name="givenFileNavigation">The navigation link of the file.</param>
        /// <param name="pageRevision">The revision of the the PAGE that the file is attached to (NOT THE FILE REVISION)</param>
        /// <returns></returns>
        [HttpGet("Page/Image/{givenPageNavigation}/{givenFileNavigation}/{pageRevision:int?}")]
        public ActionResult Image(string givenPageNavigation, string givenFileNavigation, int? pageRevision = null)
        {
            var pageNavigation = new NamespaceNavigation(givenPageNavigation);
            var fileNavigation = new NamespaceNavigation(givenFileNavigation);

            string givenScale = GetQueryValue("Scale", "100");

            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Page, [givenPageNavigation, givenFileNavigation, pageRevision, givenScale]);
            if (WikiCache.TryGet<ImageCacheItem>(cacheKey, out var cached))
            {
                return File(cached.Data, cached.ContentType);
            }

            var file = PageFileRepository.GetPageFileAttachmentByPageNavigationPageRevisionAndFileNavigation(pageNavigation.Canonical, fileNavigation.Canonical, pageRevision);

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
                        height += 16 - height;
                        width += 16 - height;
                    }
                    if (width < 16)
                    {
                        height += 16 - width;
                        width += 16 - width;
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
        /// <param name="pageRevision">The revision of the the PAGE that the file is attached to (NOT THE FILE REVISION)</param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("Page/Png/{givenPageNavigation}/{givenFileNavigation}/{pageRevision:int?}")]
        public ActionResult Png(string givenPageNavigation, string givenFileNavigation, int? pageRevision = null)
        {
            SessionState.RequireViewPermission();

            var pageNavigation = new NamespaceNavigation(givenPageNavigation);
            var fileNavigation = new NamespaceNavigation(givenFileNavigation);

            string givenScale = GetQueryValue("Scale", "100");

            var file = PageFileRepository.GetPageFileAttachmentByPageNavigationPageRevisionAndFileNavigation(pageNavigation.Canonical, fileNavigation.Canonical, pageRevision);
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
                        height += 16 - height;
                        width += 16 - height;
                    }
                    if (width < 16)
                    {
                        height += 16 - width;
                        width += 16 - width;
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
        /// <param name="givenFileNavigation">The navigation link of the file.</param>
        /// <param name="pageRevision">The revision of the the PAGE that the file is attached to (NOT THE FILE REVISION)</param>
        /// </summary>
        [AllowAnonymous]
        [HttpGet("Page/Binary/{givenPageNavigation}/{givenFileNavigation}/{pageRevision:int?}")]
        public ActionResult Binary(string givenPageNavigation, string givenFileNavigation, int? pageRevision = null)
        {
            SessionState.RequireViewPermission();

            var pageNavigation = new NamespaceNavigation(givenPageNavigation);
            var fileNavigation = new NamespaceNavigation(givenFileNavigation);

            var file = PageFileRepository.GetPageFileAttachmentByPageNavigationPageRevisionAndFileNavigation(pageNavigation.Canonical, fileNavigation.Canonical, pageRevision);

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

        #endregion
    }
}
