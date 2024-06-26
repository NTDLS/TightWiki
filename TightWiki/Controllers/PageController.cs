﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using System.Text;
using TightWiki.Library;
using TightWiki.Models.DataModels;
using TightWiki.Models.ViewModels.Page;
using TightWiki.Repository;
using TightWiki.Wiki;
using static TightWiki.Library.Constants;
using static TightWiki.Library.Images;

namespace TightWiki.Controllers
{
    [Route("")]
    public class PageController : ControllerBase
    {
        public PageController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
            : base(signInManager, userManager)
        {
        }

        #region NonAction Functions.

        [NonAction]
        private int SavePage(Page page)
        {
            bool isNewlyCreated = page.Id == 0;

            page.Id = PageRepository.SavePage(page);

            RefreshPageMetadata(this, page);

            if (isNewlyCreated)
            {
                //This will update the pageid of references that have been saved to the navigation link.
                PageRepository.UpdateSinglePageReference(page.Navigation, page.Id);
            }

            return page.Id;
        }

        [NonAction]
        public static void RefreshPageMetadata(ControllerBase controller, Page page)
        {
            var wikifier = new Wikifier(controller.WikiContext, page, null, controller.Request.Query, new WikiMatchType[] { WikiMatchType.Function });

            PageRepository.UpdatePageTags(page.Id, wikifier.Tags);
            PageRepository.UpdatePageProcessingInstructions(page.Id, wikifier.ProcessingInstructions);

            var pageTokens = wikifier.ParsePageTokens().Select(o => o.ToPageToken(page.Id)).ToList();

            PageRepository.SavePageSearchTokens(pageTokens);

            PageRepository.UpdatePageReferences(page.Id, wikifier.OutgoingLinks);

            WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.Page, [page.Id]));
            WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.Page, [page.Navigation]));
        }

        #endregion

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
            WikiContext.RequireViewPermission();

            var model = new PageDisplayViewModel();
            var navigation = new NamespaceNavigation(givenCanonical);

            var page = PageRepository.GetPageRevisionByNavigation(navigation.Canonical, pageRevision);
            if (page != null)
            {
                var instructions = PageRepository.GetPageProcessingInstructionsByPageId(page.Id);
                model.HideFooterComments = instructions.Where(o => o.Instruction == WikiInstruction.HideFooterComments).Any();
                model.HideFooterLastModified = instructions.Where(o => o.Instruction == WikiInstruction.HideFooterLastModified).Any();

                if (page.Revision == page.LatestRevision)
                {
                    pageRevision = null;
                }

                WikiContext.SetPageId(page.Id, pageRevision);

                bool allowCache = GlobalSettings.PageCacheSeconds > 0;

                if (allowCache)
                {
                    string queryKey = string.Empty;
                    foreach (var query in Request.Query)
                    {
                        queryKey += $"{query.Key}:{query.Value}";
                    }

                    var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Page, [page.Navigation, page.Revision, queryKey]);
                    var result = WikiCache.Get<string>(cacheKey);
                    if (result != null)
                    {
                        model.Body = result;
                        WikiCache.Put(cacheKey, result); //Update the cache expiration.
                    }
                    else
                    {
                        var wiki = new Wikifier(WikiContext, page, pageRevision, Request.Query);

                        model.Body = wiki.ProcessedBody;

                        if (wiki.ProcessingInstructions.Contains(WikiInstruction.NoCache) == false)
                        {
                            WikiCache.Put(cacheKey, wiki.ProcessedBody, GlobalSettings.PageCacheSeconds); //This is cleared with the call to Cache.ClearCategory($"Page:{page.Navigation}");
                        }
                    }
                }
                else
                {
                    var wiki = new Wikifier(WikiContext, page, pageRevision, Request.Query);
                    model.Body = wiki.ProcessedBody;
                }

                if (GlobalSettings.EnablePageComments && GlobalSettings.ShowCommentsOnPageFooter && model.HideFooterComments == false)
                {
                    model.Comments = PageRepository.GetPageCommentsPaged(navigation.Canonical, 1);
                }
            }
            else if (pageRevision != null)
            {
                var notExistPageName = ConfigurationRepository.Get<string>("Customization", "Revision Does Not Exists Page");
                string notExistPageNavigation = NamespaceNavigation.CleanAndValidate(notExistPageName);
                var notExistsPage = PageRepository.GetPageRevisionByNavigation(notExistPageNavigation).EnsureNotNull();

                WikiContext.SetPageId(null, pageRevision);

                var wiki = new Wikifier(WikiContext, notExistsPage, null, Request.Query);
                WikiContext.Title = notExistsPage.Name;
                model.Body = wiki.ProcessedBody;

                model.HideFooterComments = true;

                if (WikiContext.IsAuthenticated && WikiContext.CanCreate)
                {
                    WikiContext.ShouldCreatePage = false;
                }
            }
            else
            {
                var notExistPageName = ConfigurationRepository.Get<string>("Customization", "Page Not Exists Page");
                string notExistPageNavigation = NamespaceNavigation.CleanAndValidate(notExistPageName);
                var notExistsPage = PageRepository.GetPageRevisionByNavigation(notExistPageNavigation).EnsureNotNull();

                WikiContext.SetPageId(null, null);

                var wiki = new Wikifier(WikiContext, notExistsPage, null, Request.Query);
                WikiContext.Title = notExistsPage.Name;
                model.Body = wiki.ProcessedBody;

                model.HideFooterComments = true;

                if (WikiContext.IsAuthenticated && WikiContext.CanCreate)
                {
                    WikiContext.ShouldCreatePage = true;
                }
            }

            if (page != null)
            {
                model.ModifiedByUserName = page.ModifiedByUserName;
                model.ModifiedDate = WikiContext.LocalizeDateTime(page.ModifiedDate);

                if (model.Comments != null)
                {
                    model.Comments.ForEach(o =>
                    {
                        o.Body = WikifierLite.Process(o.Body);
                        o.CreatedDate = WikiContext.LocalizeDateTime(o.CreatedDate);
                    });
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
            string searchString = GetQueryString("SearchString") ?? string.Empty;
            if (string.IsNullOrEmpty(searchString) == false)
            {
                var model = new PageSearchViewModel()
                {
                    Pages = PageRepository.PageSearchPaged(Utility.SplitToTokens(searchString), GetQueryString("page", 1)),
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
            string searchString = GetQueryString("SearchString") ?? string.Empty;
            if (string.IsNullOrEmpty(searchString) == false)
            {
                model = new PageSearchViewModel()
                {
                    Pages = PageRepository.PageSearchPaged(Utility.SplitToTokens(searchString), GetQueryString("page", 1)),
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
            WikiContext.RequireViewPermission();

            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);

            var pageInfo = PageRepository.GetPageInfoByNavigation(pageNavigation);
            if (pageInfo == null)
            {
                return NotFound();
            }

            var deleteAction = GetQueryString("Delete");
            if (string.IsNullOrEmpty(deleteAction) == false && WikiContext.IsAuthenticated)
            {
                if (WikiContext.CanModerate)
                {
                    //Moderators and administrators can delete comments that they do not own.
                    PageRepository.DeletePageCommentById(pageInfo.Id, int.Parse(deleteAction));
                }
                else
                {
                    PageRepository.DeletePageCommentByUserAndId(pageInfo.Id, WikiContext.Profile.EnsureNotNull().UserId, int.Parse(deleteAction));
                }
            }

            var model = new PageCommentsViewModel()
            {
                Comments = PageRepository.GetPageCommentsPaged(pageNavigation, GetQueryString("page", 1))
            };

            model.PaginationPageCount = (model.Comments.FirstOrDefault()?.PaginationPageCount ?? 0);

            model.Comments.ForEach(o =>
            {
                o.Body = WikifierLite.Process(o.Body);
                o.CreatedDate = WikiContext.LocalizeDateTime(o.CreatedDate);
            });

            WikiContext.SetPageId(pageInfo.Id);

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
            WikiContext.RequireEditPermission();

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

            PageRepository.InsertPageComment(pageInfo.Id, WikiContext.Profile.EnsureNotNull().UserId, model.Comment);

            model = new PageCommentsViewModel()
            {
                Comments = PageRepository.GetPageCommentsPaged(pageNavigation, GetQueryString("page", 1)),
                ErrorMessage = errorMessage.DefaultWhenNull(string.Empty)
            };

            model.PaginationPageCount = (model.Comments.FirstOrDefault()?.PaginationPageCount ?? 0);

            model.Comments.ForEach(o =>
            {
                o.CreatedDate = WikiContext.LocalizeDateTime(o.CreatedDate);
            });

            WikiContext.SetPageId(pageInfo.Id);

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
                RefreshPageMetadata(this, page);
            }

            return Redirect($"/{pageNavigation}");
        }

        #endregion

        #region Revisions.

        [Authorize]
        [HttpGet("{givenCanonical}/Revisions")]
        public ActionResult Revisions(string givenCanonical)
        {
            WikiContext.RequireViewPermission();

            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);

            var model = new PageRevisionsViewModel()
            {
                Revisions = PageRepository.GetPageRevisionsInfoByNavigationPaged(pageNavigation, GetQueryString("page", 1))
            };

            model.PaginationPageCount = (model.Revisions.FirstOrDefault()?.PaginationPageCount ?? 0);

            model.Revisions.ForEach(o =>
            {
                o.CreatedDate = WikiContext.LocalizeDateTime(o.CreatedDate);
                o.ModifiedDate = WikiContext.LocalizeDateTime(o.ModifiedDate);
            });

            foreach (var p in model.Revisions)
            {
                var thisRev = PageRepository.GetPageRevisionByNavigation(p.Navigation, p.Revision);
                var prevRev = PageRepository.GetPageRevisionByNavigation(p.Navigation, p.Revision - 1);
                p.ChangeSummary = Differentiator.GetComparisonSummary(thisRev?.Body ?? "", prevRev?.Body ?? "");
            }

            if (model.Revisions != null && model.Revisions.Count > 0)
            {
                WikiContext.SetPageId(model.Revisions.First().PageId);
            }

            return View(model);
        }

        #endregion

        #region Delete.

        [Authorize]
        [HttpPost("{givenCanonical}/Delete")]
        public ActionResult Delete(string givenCanonical, PageDeleteViewModel model)
        {
            WikiContext.RequireDeletePermission();

            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);

            var page = PageRepository.GetPageRevisionByNavigation(pageNavigation);
            var instructions = PageRepository.GetPageProcessingInstructionsByPageId(page.EnsureNotNull().Id);
            if (instructions.Any(o => o.Instruction == WikiInstruction.Protect))
            {
                model.ErrorMessage = "The page is protected and cannot be deleted. A moderator or an administrator must remove the protection before deletion.";
                return View(model);
            }

            bool confirmAction = bool.Parse(GetFormString("IsActionConfirmed").EnsureNotNull());
            if (confirmAction == true && page != null)
            {
                PageRepository.MovePageToDeletedById(page.Id, (WikiContext.Profile?.UserId).EnsureNotNullOrEmpty());
                WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.Page, [page.Navigation]));
                WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.Page, [page.Id]));
                return NotifyOfSuccessAction("The page has been deleted successfully!", $"/Home");
            }

            return Redirect($"/{pageNavigation}");
        }

        [Authorize]
        [HttpGet("{givenCanonical}/Delete")]
        public ActionResult Delete(string givenCanonical)
        {
            WikiContext.RequireDeletePermission();

            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);

            var page = PageRepository.GetPageRevisionByNavigation(pageNavigation).EnsureNotNull();

            var model = new PageDeleteViewModel()
            {
                CountOfAttachments = PageRepository.GetCountOfPageAttachmentsById(page.Id),
                PageName = page.Name,
                MostCurrentRevision = page.Revision,
                PageRevision = page.Revision
            };

            WikiContext.SetPageId(page.Id);

            var instructions = PageRepository.GetPageProcessingInstructionsByPageId(page.Id);
            if (instructions.Any(o => o.Instruction == WikiInstruction.Protect))
            {
                model.ErrorMessage = "The page is protected and cannot be deleted. A moderator or an administrator must remove the protection before deletion.";
                return View(model);
            }

            return View(model);
        }

        #endregion

        #region Revert.

        [Authorize]
        [HttpPost("{givenCanonical}/Revert/{pageRevision:int}")]
        public ActionResult Revert(string givenCanonical, int pageRevision, PageRevertViewModel model)
        {
            WikiContext.RequireModeratePermission();

            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);

            bool confirmAction = bool.Parse(GetFormString("IsActionConfirmed").EnsureNotNullOrEmpty());
            if (confirmAction == true)
            {
                var page = PageRepository.GetPageRevisionByNavigation(pageNavigation, pageRevision).EnsureNotNull();
                SavePage(page);
                return NotifyOfSuccessAction("The page has been reverted successfully!", $"/{pageNavigation}");
            }

            return Redirect($"/{pageNavigation}");
        }

        [Authorize]
        [HttpGet("{givenCanonical}/Revert/{pageRevision:int}")]
        public ActionResult Revert(string givenCanonical, int pageRevision)
        {
            WikiContext.RequireModeratePermission();

            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);

            var mostCurrentPage = PageRepository.GetPageRevisionByNavigation(pageNavigation).EnsureNotNull();
            mostCurrentPage.CreatedDate = WikiContext.LocalizeDateTime(mostCurrentPage.CreatedDate);
            mostCurrentPage.ModifiedDate = WikiContext.LocalizeDateTime(mostCurrentPage.ModifiedDate);

            var revisionPage = PageRepository.GetPageRevisionByNavigation(pageNavigation, pageRevision).EnsureNotNull();
            revisionPage.CreatedDate = WikiContext.LocalizeDateTime(revisionPage.CreatedDate);
            revisionPage.ModifiedDate = WikiContext.LocalizeDateTime(revisionPage.ModifiedDate);

            var model = new PageRevertViewModel()
            {
                PageName = revisionPage.Name,
                CountOfRevisions = mostCurrentPage.Revision - revisionPage.Revision,
                MostCurrentRevision = mostCurrentPage.Revision,
            };

            if (revisionPage != null)
            {
                WikiContext.SetPageId(revisionPage.Id, pageRevision);
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
            WikiContext.RequireEditPermission();

            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);

            var page = PageRepository.GetPageRevisionByNavigation(pageNavigation);
            if (page != null)
            {
                var instructions = PageRepository.GetPageProcessingInstructionsByPageId(page.EnsureNotNull().Id);
                if (WikiContext.CanModerate == false && instructions.Any(o => o.Instruction == WikiInstruction.Protect))
                {
                    return NotifyOfError("The page is protected and cannot be modified except by a moderator or an administrator unless the protection is removed.");
                }

                WikiContext.SetPageId(page.Id);

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
                var pageName = GetQueryString("Name").DefaultWhenNullOrEmpty(pageNavigation);

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
            WikiContext.RequireEditPermission();

            if (!model.ValidateModelAndSetErrors(ModelState))
            {
                return View(model);
            }

            if (model.Id == 0) //Saving a new page.
            {
                var page = new Page()
                {
                    CreatedDate = DateTime.UtcNow,
                    CreatedByUserId = WikiContext.Profile.EnsureNotNull().UserId,
                    ModifiedDate = DateTime.UtcNow,
                    ModifiedByUserId = WikiContext.Profile.UserId,
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

                page.Id = SavePage(page);

                WikiContext.SetPageId(page.Id);

                return NotifyOfSuccessAction("The page has been created successfully!", $"/{page.Navigation}/Edit");
            }
            else
            {
                var page = PageRepository.GetPageRevisionById(model.Id).EnsureNotNull();
                var instructions = PageRepository.GetPageProcessingInstructionsByPageId(page.Id);
                if (WikiContext.CanModerate == false && instructions.Any(o => o.Instruction == WikiInstruction.Protect))
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
                page.ModifiedByUserId = WikiContext.Profile.EnsureNotNull().UserId;
                page.Body = model.Body ?? "";
                page.Name = model.Name;
                page.Navigation = NamespaceNavigation.CleanAndValidate(model.Name);
                page.Description = model.Description ?? "";

                SavePage(page);

                WikiContext.SetPageId(page.Id);

                model.SuccessMessage = "The page was saved successfully!";

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

            string givenScale = GetQueryString("Scale", "100");
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
                        format = ImageFormat.Gif;
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
        /// <param name="givenFileNavigation">The navigation link of the file.</param>
        /// <param name="pageRevision">The revision of the the PAGE that the file is attached to (NOT THE FILE REVISION)</param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("Page/Png/{givenPageNavigation}/{givenFileNavigation}/{pageRevision:int?}")]
        public ActionResult Png(string givenPageNavigation, string givenFileNavigation, int? pageRevision = null)
        {
            WikiContext.RequireViewPermission();

            var pageNavigation = new NamespaceNavigation(givenPageNavigation);
            var fileNavigation = new NamespaceNavigation(givenFileNavigation);

            string givenScale = GetQueryString("Scale", "100");

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
            WikiContext.RequireViewPermission();

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
