using DiffPlex.DiffBuilder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NTDLS.Helpers;
using SixLabors.ImageSharp;
using System.Globalization;
using System.Text;
using System.Xml.Serialization;
using TightWiki.Engine;
using TightWiki.Library.Security;
using TightWiki.Plugin;
using TightWiki.Plugin.Caching;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Library;
using TightWiki.Plugin.Models;
using TightWiki.RequestModels;
using TightWiki.ViewModels.Page;
using static TightWiki.Plugin.Library.TwImages;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Controllers
{
    [Route("")]
    public class PageController(
            ILogger<ITwEngine> logger,
            ISideBySideDiffBuilder diffBuilder,
            ITwConfigurationRepository configurationRepository,
            ITwEngine tightEngine,
            ITwPageRepository pageRepository,
            ITwSharedLocalizationText localizer,
            ITwStatisticsRepository statisticsRepository,
            ITwUsersRepository usersRepository,
            SignInManager<IdentityUser> signInManager,
            TwConfiguration wikiConfiguration,
            UserManager<IdentityUser> userManager,
            ITwDatabaseManager databaseManager
        )
        : TwController<PageController>(logger, signInManager, userManager, localizer, wikiConfiguration, databaseManager)
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

        [Authorize]
        [Route("/ping")]
        public JsonResult Ping()
        {
            return Json(new { now = DateTime.UtcNow });
        }

        #region Display.

        /// <summary>
        /// Default controller for root requests.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult> Display()
            => await Display("home", null);

        [NonAction]
        private async Task TryIncrementPageView(int pageId)
        {
            try
            {
                var hitCounterThrottle = await configurationRepository.Get(WikiConfigurationGroup.Performance, "Hit Counter Throttle (Seconds)", 0);
                var cacheKey = TwCacheKey.Build(TwCache.Category.Session, [pageId, "PageView", GetPageViewViewerKey()]);
                if (TwCache.Contains(cacheKey))
                {
                    return;
                }

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await statisticsRepository.IncrementPageViewCount(pageId);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Failed to increment page view count for page id {PageId} in background task.", pageId);
                    }
                });

                //Just store a value (in this case a DateTime) so that we can test for it later.
                TwCache.Set(cacheKey, DateTime.UtcNow, TimeSpan.FromSeconds(hitCounterThrottle));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to increment page view count for page id {PageId}", pageId);
            }
        }

        [NonAction]
        private string GetPageViewViewerKey()
        {
            if (User.Identity?.IsAuthenticated == true && SessionState.Profile != null)
            {
                return SecurityUtility.Sha1($"user:{SessionState.Profile.UserId}");
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            //IP could be a load balancer or somehting like cloudflare,
            //  so lets bake in the user agent as well to help differentiate viewers.
            var userAgent = Request.Headers.UserAgent.ToString();

            return SecurityUtility.Sha1($"anon:{ip}:{userAgent}");
        }

        [HttpGet("{givenCanonical}/{pageRevision:int?}")]
        public async Task<ActionResult> Display(string givenCanonical, int? pageRevision)
        {
            try
            {
                await SessionState.RequirePermission(givenCanonical, WikiPermission.Read);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }

            try
            {
                var model = new PageDisplayViewModel();
                var navigation = new TwNamespaceNavigation(givenCanonical);

                var page = await pageRepository.GetPageRevisionByNavigation(navigation.Canonical, pageRevision);
                if (page != null)
                {
                    await TryIncrementPageView(page.Id);

                    var instructions = await pageRepository.GetPageProcessingInstructionsByPageId(page.Id);
                    model.Revision = page.Revision;
                    model.MostCurrentRevision = page.MostCurrentRevision;
                    model.Name = page.Name;
                    model.Namespace = page.Namespace;
                    model.Navigation = page.Navigation;
                    model.HideFooterComments = instructions.Contains(WikiInstruction.HideFooterComments);
                    model.HideFooterLastModified = instructions.Contains(WikiInstruction.HideFooterLastModified);
                    model.ModifiedByUserName = page.ModifiedByUserName;
                    model.ModifiedDate = SessionState.LocalizeDateTime(page.ModifiedDate);

                    await SessionState.SetPageId(page.Id, pageRevision);

                    if (WikiConfiguration.PageCacheSeconds > 0)
                    {
                        string queryKey = string.Empty;
                        foreach (var query in Request.Query)
                        {
                            queryKey += $"{query.Key}:{query.Value}";
                        }

                        var cacheKey = TwCacheKeyFunction.Build(TwCache.Category.Page, [page.Navigation, page.Revision, queryKey]);
                        if (TwCache.TryGet<TwPageCache>(cacheKey, out var cached))
                        {
                            model.Body = cached.Body;
                            SessionState.PageTitle = cached.PageTitle;
                            TwCache.Set(cacheKey, cached); //Update the cache expiration.
                        }
                        else
                        {
                            var state = await tightEngine.Transform(Localizer, SessionState, page, pageRevision);
                            SessionState.PageTitle = state.PageTitle;

                            model.Body = state.HtmlResult;
                            if (state.ProcessingInstructions.Contains(WikiInstruction.NoCache) == false)
                            {
                                var toBeCached = new TwPageCache(state.HtmlResult)
                                {
                                    PageTitle = state.PageTitle
                                };

                                TwCache.Set(cacheKey, toBeCached); //This is cleared with the call to Cache.ClearCategory($"Page:{page.Navigation}");
                            }
                        }
                    }
                    else
                    {
                        var state = await tightEngine.Transform(Localizer, SessionState, page, pageRevision);

                        model.Body = state.HtmlResult;
                    }

                    if (WikiConfiguration.EnablePageComments && WikiConfiguration.ShowCommentsOnPageFooter && model.HideFooterComments == false)
                    {
                        var comments = await pageRepository.GetPageCommentsPaged(navigation.Canonical, 1);

                        foreach (var comment in comments)
                        {
                            model.Comments.Add(new TwPageComment
                            {
                                PaginationPageCount = comment.PaginationPageCount,
                                UserNavigation = comment.UserNavigation,
                                Id = comment.Id,
                                UserName = comment.UserName,
                                UserId = comment.UserId,
                                Body = TwWikifierLite.Process(WikiConfiguration, comment.Body),
                                CreatedDate = SessionState.LocalizeDateTime(comment.CreatedDate)
                            });
                        }
                    }
                }
                else if (pageRevision != null)
                {
                    var notExistPageName = await configurationRepository.Get<string>(WikiConfigurationGroup.Customization, "Revision Does Not Exists Page");
                    string notExistPageNavigation = TwNamespaceNavigation.CleanAndValidate(notExistPageName);
                    var notExistsPage = (await pageRepository.GetPageRevisionByNavigation(notExistPageNavigation)).EnsureNotNull();

                    await SessionState.SetPageId(null, pageRevision);

                    var state = await tightEngine.Transform(Localizer, SessionState, notExistsPage);

                    SessionState.Page.Name = notExistsPage.Name;
                    model.Body = state.HtmlResult;

                    model.HideFooterComments = true;

                    if (SessionState.IsAuthenticated && await SessionState.HoldsPermission(givenCanonical, WikiPermission.Create))
                    {
                        SessionState.ShouldCreatePage = false;
                    }
                }
                else
                {
                    var notExistPageName = await configurationRepository.Get<string>(WikiConfigurationGroup.Customization, "Page Not Exists Page");
                    string notExistPageNavigation = TwNamespaceNavigation.CleanAndValidate(notExistPageName);
                    var notExistsPage = (await pageRepository.GetPageRevisionByNavigation(notExistPageNavigation)).EnsureNotNull();

                    await SessionState.SetPageId(null, null);

                    var state = await tightEngine.Transform(Localizer, SessionState, notExistsPage);

                    SessionState.Page.Name = notExistsPage.Name;
                    model.Body = state.HtmlResult;

                    model.HideFooterComments = true;

                    if (SessionState.IsAuthenticated && await SessionState.HoldsPermission(givenCanonical, WikiPermission.Create))
                    {
                        SessionState.ShouldCreatePage = true;
                    }
                }

                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while displaying page with navigation {Navigation} and revision {Revision}.", givenCanonical, pageRevision);
                throw;
            }
        }

        #endregion

        #region Search.

        [Authorize]
        [HttpGet("Page/AutoCompletePage")]
        public async Task<ActionResult> AutoCompletePage([FromQuery] string? q = null)
        {
            try
            {
                var pages = await pageRepository.AutoCompletePage(q);

                return Json(pages.Select(o => new
                {
                    text = o.Name,
                    id = o.Navigation
                }));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while auto-completing page names for query {Query}.", q);
                throw;
            }
        }

        [AllowAnonymous]
        [HttpGet("Page/Search")]
        public async Task<ActionResult> Search()
        {
            try
            {
                string searchString = GetQueryValue("SearchString", string.Empty);
                if (string.IsNullOrEmpty(searchString) == false)
                {
                    var model = new PageSearchViewModel()
                    {
                        Pages = await pageRepository.PageSearchPaged(Utility.SplitToTokens(searchString), GetQueryValue("page", 1)),
                        SearchString = searchString
                    };

                    model.PaginationPageCount = (model.Pages.FirstOrDefault()?.PaginationPageCount ?? 0);

                    return View(model);
                }

                return View(new PageSearchViewModel()
                {
                    Pages = new(),
                    SearchString = searchString
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while searching pages");
                throw;
            }
        }

        [AllowAnonymous]
        [HttpPost("Page/Search")]
        public async Task<ActionResult> Search(PageSearchViewModel model)
        {
            try
            {
                string searchString = GetQueryValue("SearchString", string.Empty);
                if (string.IsNullOrEmpty(searchString) == false)
                {
                    model = new PageSearchViewModel()
                    {
                        Pages = await pageRepository.PageSearchPaged(Utility.SplitToTokens(searchString), GetQueryValue("page", 1)),
                        SearchString = searchString
                    };

                    model.PaginationPageCount = (model.Pages.FirstOrDefault()?.PaginationPageCount ?? 0);

                    return View(model);
                }

                return View(new PageSearchViewModel()
                {
                    Pages = new(),
                    SearchString = searchString
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while searching pages with search string {SearchString}.", model.SearchString);
                throw;
            }
        }

        #endregion

        #region Localization

        [AllowAnonymous]
        [HttpGet("Page/Localization")]
        public async Task<ActionResult> Localization([FromServices] IOptions<RequestLocalizationOptions> localizationOptions)
        {
            try
            {
                var referrer = Request.Headers.Referer.ToString();
                ViewBag.ReturnUrl = string.IsNullOrEmpty(referrer) ? "" : referrer;

                var languages = localizationOptions.Value.SupportedUICultures.EnsureNotNull()
                    .OrderBy(x => x.EnglishName, StringComparer.Create(CultureInfo.CurrentUICulture, ignoreCase: true)).ToList();

                return View(new PageLocalizationViewModel { Languages = languages });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while loading the localization settings page.");
                throw;
            }

        }


        [AllowAnonymous]
        [HttpGet("Page/SetLocalization")]
        public async Task<ActionResult> SetLocalization([FromServices] IOptions<RequestLocalizationOptions> localizationOptions, string culture, string returnUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(culture) || string.IsNullOrWhiteSpace(returnUrl))
                    return BadRequest();

                if (SessionState.IsAuthenticated)
                {
                    var userId = SessionState.Profile.EnsureNotNull().UserId;
                    var profile = await usersRepository.GetAccountProfileByUserId(userId);
                    profile.Language = culture;
                    await usersRepository.UpdateProfile(profile);
                }

                Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName,
                        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                    new CookieOptions
                    {
                        Expires = DateTimeOffset.UtcNow.AddYears(1),
                        IsEssential = true,
                        SameSite = SameSiteMode.Lax,
                        Secure = true,
                        HttpOnly = false
                    }
                );

                return Redirect(returnUrl);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while setting localization with culture {Culture} and return URL {ReturnUrl}.", culture, returnUrl);
                throw;
            }

        }

        #endregion

        #region Comments.

        [AllowAnonymous]
        [HttpGet("{givenCanonical}/Comments")]
        public async Task<ActionResult> Comments(string givenCanonical)
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(givenCanonical, WikiPermission.Read);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                var pageNavigation = TwNamespaceNavigation.CleanAndValidate(givenCanonical);

                var pageInfo = await pageRepository.GetPageInfoByNavigation(pageNavigation);
                if (pageInfo == null)
                {
                    return NotFound();
                }

                var deleteAction = GetQueryValue<string>("Delete");
                if (string.IsNullOrEmpty(deleteAction) == false && SessionState.IsAuthenticated)
                {
                    if (await SessionState.HoldsPermission(givenCanonical, WikiPermission.Moderate))
                    {
                        //Moderators and administrators can delete comments that they do not own.
                        await pageRepository.DeletePageCommentById(pageInfo.Id, int.Parse(deleteAction));
                    }
                    else
                    {
                        await pageRepository.DeletePageCommentByUserAndId(pageInfo.Id, SessionState.Profile.EnsureNotNull().UserId, int.Parse(deleteAction));
                    }
                }

                var model = new PageCommentsViewModel();

                var comments = await pageRepository.GetPageCommentsPaged(pageNavigation, GetQueryValue("page", 1));
                foreach (var comment in comments)
                {
                    model.Comments.Add(new TwPageComment
                    {
                        PaginationPageCount = comment.PaginationPageCount,
                        UserNavigation = comment.UserNavigation,
                        Id = comment.Id,
                        UserName = comment.UserName,
                        UserId = comment.UserId,
                        Body = TwWikifierLite.Process(WikiConfiguration, comment.Body),
                        CreatedDate = SessionState.LocalizeDateTime(comment.CreatedDate)
                    });
                }

                model.PaginationPageCount = (model.Comments.FirstOrDefault()?.PaginationPageCount ?? 0);

                await SessionState.SetPageId(pageInfo.Id);

                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while loading comments for page with navigation {Navigation}.", givenCanonical);
                throw;
            }

        }

        /// <summary>
        /// Insert new page comment.
        /// </summary>
        [Authorize]
        [HttpPost("{givenCanonical}/Comments")]
        public async Task<ActionResult> Comments(PageCommentsViewModel model, string givenCanonical)
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(givenCanonical, WikiPermission.Edit);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                string? errorMessage = null;

                var pageNavigation = TwNamespaceNavigation.CleanAndValidate(givenCanonical);

                var pageInfo = await pageRepository.GetPageInfoByNavigation(pageNavigation);
                if (pageInfo == null)
                {
                    return NotFound();
                }

                await pageRepository.InsertPageComment(pageInfo.Id, SessionState.Profile.EnsureNotNull().UserId, model.Comment);

                model = new PageCommentsViewModel()
                {
                    ErrorMessage = errorMessage.DefaultWhenNull(string.Empty)
                };

                var comments = await pageRepository.GetPageCommentsPaged(pageNavigation, GetQueryValue("page", 1));
                foreach (var comment in comments)
                {
                    model.Comments.Add(new TwPageComment
                    {
                        PaginationPageCount = comment.PaginationPageCount,
                        UserNavigation = comment.UserNavigation,
                        Id = comment.Id,
                        UserName = comment.UserName,
                        UserId = comment.UserId,
                        Body = TwWikifierLite.Process(WikiConfiguration, comment.Body),
                        CreatedDate = SessionState.LocalizeDateTime(comment.CreatedDate)
                    });
                }

                model.PaginationPageCount = (model.Comments.FirstOrDefault()?.PaginationPageCount ?? 0);

                await SessionState.SetPageId(pageInfo.Id);

                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while posting a comment for page with navigation {Navigation}.", givenCanonical);
                throw;
            }

        }

        #endregion

        #region Refresh.

        [Authorize]
        [HttpGet("{givenCanonical}/Refresh")]
        public async Task<ActionResult> Refresh(string givenCanonical)
        {
            try
            {
                var pageNavigation = TwNamespaceNavigation.CleanAndValidate(givenCanonical);

                var page = await pageRepository.GetPageRevisionByNavigation(pageNavigation, null, true);

                if (page != null)
                {
                    await pageRepository.RefreshPageMetadata(tightEngine, Localizer, page, SessionState);
                }

                return Redirect($"{WikiConfiguration.BasePath}/{pageNavigation}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while refreshing page metadata for page with navigation {Navigation}.", givenCanonical);
                throw;
            }

        }

        #endregion

        #region Compare.

        [Authorize]
        [HttpGet("{givenCanonical}/Compare/{pageRevision:int}")]
        public async Task<ActionResult> Compare(string givenCanonical, int pageRevision)
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(givenCanonical, WikiPermission.Read);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                var pageNavigation = TwNamespaceNavigation.CleanAndValidate(givenCanonical);

                var thisRev = await pageRepository.GetPageRevisionByNavigation(pageNavigation, pageRevision);
                var prevRev = await pageRepository.GetPageRevisionByNavigation(pageNavigation, pageRevision - 1);

                if (thisRev != null)
                {
                    await SessionState.SetPageId(thisRev.Id, pageRevision);
                }

                var model = new PageCompareViewModel()
                {
                    MostCurrentRevision = thisRev?.MostCurrentRevision,
                    ModifiedByUserName = thisRev?.ModifiedByUserName ?? string.Empty,
                    ThisRevision = thisRev?.Revision,
                    PreviousRevision = prevRev?.Revision,
                    DiffModel = diffBuilder.BuildDiffModel(prevRev?.Body ?? string.Empty, thisRev?.Body ?? string.Empty),
                    ModifiedDate = SessionState.LocalizeDateTime(thisRev?.ModifiedDate ?? DateTime.MinValue),
                    ChangeSummary = thisRev?.ChangeSummary ?? string.Empty,
                    ChangeAnalysis = TwDifferentiator.GetComparisonSummary(thisRev?.Body ?? "", prevRev?.Body ?? "")
                };

                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while comparing revisions for page with navigation {Navigation} and revision {Revision}.", givenCanonical, pageRevision);
                throw;
            }
        }

        #endregion

        #region Revisions.

        [Authorize]
        [HttpGet("{givenCanonical}/Revisions")]
        public async Task<ActionResult> Revisions(string givenCanonical)
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(givenCanonical, WikiPermission.Read);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                var pageNavigation = TwNamespaceNavigation.CleanAndValidate(givenCanonical);

                var pageNumber = GetQueryValue("page", 1);
                var orderBy = GetQueryValue<string>("OrderBy");
                var orderByDirection = GetQueryValue<string>("OrderByDirection");

                var model = new RevisionsViewModel()
                {
                    Revisions = await pageRepository.GetPageRevisionsInfoByNavigationPaged(pageNavigation, pageNumber, orderBy, orderByDirection)
                };

                model.PaginationPageCount = (model.Revisions.FirstOrDefault()?.PaginationPageCount ?? 0);

                model.Revisions.ForEach(o =>
                {
                    o.CreatedDate = SessionState.LocalizeDateTime(o.CreatedDate);
                    o.ModifiedDate = SessionState.LocalizeDateTime(o.ModifiedDate);
                });

                foreach (var p in model.Revisions)
                {
                    var thisRev = await pageRepository.GetPageRevisionByNavigation(p.Navigation, p.Revision);
                    var prevRev = await pageRepository.GetPageRevisionByNavigation(p.Navigation, p.Revision - 1);
                    p.ChangeAnalysis = TwDifferentiator.GetComparisonSummary(thisRev?.Body ?? "", prevRev?.Body ?? "");
                }

                if (model.Revisions != null && model.Revisions.Count > 0)
                {
                    await SessionState.SetPageId(model.Revisions.First().PageId);
                }

                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while loading revisions for page with navigation {Navigation}.", givenCanonical);
                throw;
            }
        }

        #endregion

        #region Delete.

        [Authorize]
        [HttpPost("{givenCanonical}/Delete")]
        public async Task<ActionResult> Delete(string givenCanonical, PageDeleteViewModel model)
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(givenCanonical, WikiPermission.Delete);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                var pageNavigation = TwNamespaceNavigation.CleanAndValidate(givenCanonical);

                var page = await pageRepository.GetPageRevisionByNavigation(pageNavigation);
                var instructions = await pageRepository.GetPageProcessingInstructionsByPageId(page.EnsureNotNull().Id);
                if (instructions.Contains(WikiInstruction.Protect))
                {
                    return NotifyOfError(Localize("The page is protected and cannot be deleted. A moderator or an administrator must remove the protection before deletion."));
                }

                bool confirmAction = bool.Parse(GetFormValue("IsActionConfirmed").EnsureNotNull());
                if (confirmAction == true && page != null)
                {
                    await pageRepository.MovePageToDeletedById(page.Id, (SessionState.Profile?.UserId).EnsureNotNullOrEmpty());
                    TwCache.ClearCategory(TwCacheKey.Build(TwCache.Category.Page, [page.Navigation]));
                    TwCache.ClearCategory(TwCacheKey.Build(TwCache.Category.Page, [page.Id]));
                    return NotifyOfSuccess(Localize("The page has been deleted."), $"/Home");
                }

                return Redirect($"{WikiConfiguration.BasePath}/{pageNavigation}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while deleting page with navigation {Navigation}.", givenCanonical);
                throw;
            }
        }

        [Authorize]
        [HttpGet("{givenCanonical}/Delete")]
        public async Task<ActionResult> Delete(string givenCanonical)
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(givenCanonical, WikiPermission.Delete);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                var pageNavigation = TwNamespaceNavigation.CleanAndValidate(givenCanonical);
                var page = (await pageRepository.GetPageRevisionByNavigation(pageNavigation)).EnsureNotNull();

                var model = new PageDeleteViewModel()
                {
                    CountOfAttachments = await pageRepository.GetCountOfPageAttachmentsById(page.Id),
                    PageName = page.Name,
                    MostCurrentRevision = page.Revision,
                    PageRevision = page.Revision
                };

                await SessionState.SetPageId(page.Id);

                var instructions = await pageRepository.GetPageProcessingInstructionsByPageId(page.Id);
                if (instructions.Contains(WikiInstruction.Protect))
                {
                    return NotifyOfError(Localize("The page is protected and cannot be deleted. A moderator or an administrator must remove the protection before deletion."));
                }

                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while loading the delete confirmation page for page with navigation {Navigation}.", givenCanonical);
                throw;
            }
        }

        #endregion

        #region Revert.

        [Authorize]
        [HttpPost("{givenCanonical}/Revert/{pageRevision:int}")]
        public async Task<ActionResult> Revert(string givenCanonical, int pageRevision, PageRevertViewModel model)
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(givenCanonical, WikiPermission.Moderate);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                var pageNavigation = TwNamespaceNavigation.CleanAndValidate(givenCanonical);

                bool confirmAction = bool.Parse(GetFormValue("IsActionConfirmed").EnsureNotNullOrEmpty());
                if (confirmAction == true)
                {
                    var page = (await pageRepository.GetPageRevisionByNavigation(pageNavigation, pageRevision)).EnsureNotNull();
                    await pageRepository.UpsertPage(tightEngine, Localizer, page, SessionState);
                    return NotifyOfSuccess(Localize("The page has been reverted."), $"/{pageNavigation}");
                }

                return Redirect($"{WikiConfiguration.BasePath}/{pageNavigation}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while reverting page with navigation {Navigation} to revision {Revision}.", givenCanonical, pageRevision);
                throw;
            }
        }

        [Authorize]
        [HttpGet("{givenCanonical}/Revert/{pageRevision:int}")]
        public async Task<ActionResult> Revert(string givenCanonical, int pageRevision)
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(givenCanonical, WikiPermission.Moderate);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                var pageNavigation = TwNamespaceNavigation.CleanAndValidate(givenCanonical);

                var mostCurrentPage = (await pageRepository.GetPageRevisionByNavigation(pageNavigation)).EnsureNotNull();
                mostCurrentPage.CreatedDate = SessionState.LocalizeDateTime(mostCurrentPage.CreatedDate);
                mostCurrentPage.ModifiedDate = SessionState.LocalizeDateTime(mostCurrentPage.ModifiedDate);

                var revisionPage = (await pageRepository.GetPageRevisionByNavigation(pageNavigation, pageRevision)).EnsureNotNull();
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
                    await SessionState.SetPageId(revisionPage.Id, pageRevision);
                }

                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while loading the revert confirmation page for page with navigation {Navigation} and revision {Revision}.", givenCanonical, pageRevision);
                throw;
            }
        }

        #endregion

        #region Edit.

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Preview([FromBody] PagePreviewRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Body))
                {
                    return Json(new
                    {
                        success = true,
                        html = "<em>No content to preview.</em>"
                    });
                }

                //We're really only showing the text that was passed in, so this check is wholly unnecessary.
                /*
                try
                {
                    //If the page exists, user must have read access to preview since we will show the existing page content with the new content.
                    var existingPage = pageRepository.GetPageRevisionByNavigation(request.PageNavigation);
                    if (existingPage != null)
                    {
                        SessionState.RequirePermission(request.PageNavigation, WikiPermission.Read);
                    }
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                */

                var page = new TwPage()
                {
                    Body = request.Body,
                    Name = request.Name,
                    Navigation = request.PageNavigation,
                };

                var state = await tightEngine.Transform(Localizer, SessionState, page);

                return Json(new
                {
                    success = true,
                    html = state.HtmlResult
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while previewing page content for page with navigation {Navigation}.", request.PageNavigation);

                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [Authorize]
        [HttpGet("{givenCanonical}/Create")]
        [HttpGet("{givenCanonical}/Edit")]
        [HttpGet("Page/Create")]
        public async Task<ActionResult> Edit(string givenCanonical)
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(givenCanonical, WikiPermission.Edit);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                var pageNavigation = TwNamespaceNavigation.CleanAndValidate(givenCanonical);

                var featureTemplates = await pageRepository.GetAllFeatureTemplates();

                var page = await pageRepository.GetPageRevisionByNavigation(pageNavigation);
                if (page != null)
                {
                    var instructions = await pageRepository.GetPageProcessingInstructionsByPageId(page.EnsureNotNull().Id);
                    if (instructions.Contains(WikiInstruction.Protect) && !await SessionState.HoldsPermission(givenCanonical, WikiPermission.Moderate))
                    {
                        return NotifyOfError(Localize("The page is protected and cannot be modified except by a moderator or an administrator unless the protection is removed."));
                    }

                    await SessionState.SetPageId(page.Id);

                    return View(new PageEditViewModel()
                    {
                        Id = page.Id,
                        Body = page.Body,
                        Name = page.Name,
                        Navigation = TwNamespaceNavigation.CleanAndValidate(page.Navigation),
                        Description = page.Description,
                        FeatureTemplates = featureTemplates
                    });
                }
                else
                {
                    var pageName = GetQueryValue<string>("Name").DefaultWhenNullOrEmpty(pageNavigation);

                    string templateName = (await configurationRepository.Get<string>(WikiConfigurationGroup.Customization, "New Page Template")).EnsureNotNull();
                    string templateNavigation = TwNamespaceNavigation.CleanAndValidate(templateName);
                    var templatePage = await pageRepository.GetPageRevisionByNavigation(templateNavigation);

                    var templates = await pageRepository.GetAllTemplatePages();

                    if (templatePage == null)
                        templatePage = new TwPage();
                    else
                        templates.Insert(0, templatePage);

                    return View(new PageEditViewModel()
                    {
                        Body = templatePage.Body,
                        Name = pageName?.Replace('_', ' ') ?? string.Empty,
                        Navigation = TwNamespaceNavigation.CleanAndValidate(pageNavigation),
                        Templates = templates,
                        FeatureTemplates = featureTemplates
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while loading the edit page for page with navigation {Navigation}.", givenCanonical);
                throw;
            }
        }

        [Authorize]
        [HttpPost("{givenCanonical}/Create")]
        [HttpPost("{givenCanonical}/Edit")]
        [HttpPost("Page/Create")]
        public async Task<ActionResult> Edit(PageEditViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                model.FeatureTemplates = await pageRepository.GetAllFeatureTemplates();

                if (WikiConfiguration.ShowChangeSummaryWhenEditing
                    && WikiConfiguration.RequireChangeSummaryWhenEditing
                    && string.IsNullOrEmpty(model.ChangeSummary))
                {
                    ModelState.AddModelError("ChangeSummary", Localize("A change summary is required for page edits."));
                    return View(model);
                }

                if (Utility.PageNameContainsUnsafeCharacters(model.Name))
                {
                    ModelState.AddModelError("Name", Localize("The page name contains characters which are disallowed: {0}.",
                        string.Join(' ', Utility.UnsafePageNameCharacters)));
                    return View(model);
                }

                if (Utility.CountOccurrencesOf(model.Name, "::") > 1)
                {
                    ModelState.AddModelError("Name", Localize("The characters '::' are used to denote a namespace name. A page name cannot contain more than one set of these characters."));
                    return View(model);
                }

                if (model.Id == 0) //Saving a new page.
                {
                    var navigation = TwNamespaceNavigation.CleanAndValidate(model.Name);

                    try
                    {
                        await SessionState.RequirePermission(navigation, WikiPermission.Create);
                    }
                    catch (Exception ex)
                    {
                        return NotifyOfError(ex.GetBaseException().Message, "/");
                    }
                    var page = new TwPage()
                    {
                        CreatedDate = DateTime.UtcNow,
                        CreatedByUserId = SessionState.Profile.EnsureNotNull().UserId,
                        ModifiedDate = DateTime.UtcNow,
                        ModifiedByUserId = SessionState.Profile.UserId,
                        Body = model.Body ?? "",
                        Name = model.Name,
                        ChangeSummary = model.ChangeSummary ?? string.Empty,
                        Navigation = navigation,
                        Description = model.Description ?? ""
                    };

                    if (await pageRepository.GetPageInfoByNavigation(page.Navigation) != null)
                    {
                        ModelState.AddModelError("Name", Localize("The page name you entered already exists."));
                        return View(model);
                    }

                    page.Id = await pageRepository.UpsertPage(tightEngine, Localizer, page, SessionState);

                    await SessionState.SetPageId(page.Id);

                    return NotifyOfSuccess(Localize("The page has been created."), $"/{page.Navigation}/Edit");
                }
                else
                {
                    var navigation = TwNamespaceNavigation.CleanAndValidate(model.Name);

                    try
                    {
                        await SessionState.RequirePermission(navigation, WikiPermission.Edit);
                    }
                    catch (Exception ex)
                    {
                        return NotifyOfError(ex.GetBaseException().Message, "/");
                    }
                    var page = (await pageRepository.GetPageRevisionById(model.Id)).EnsureNotNull();
                    var instructions = await pageRepository.GetPageProcessingInstructionsByPageId(page.Id);
                    if (instructions.Contains(WikiInstruction.Protect) && !await SessionState.HoldsPermission(navigation, WikiPermission.Moderate))
                    {
                        return NotifyOfError(Localize("The page is protected and cannot be modified except by a moderator or an administrator unless the protection is removed."));
                    }

                    string originalNavigation = string.Empty;

                    model.Navigation = navigation;

                    if (!page.Navigation.Equals(model.Navigation, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (await pageRepository.GetPageInfoByNavigation(model.Navigation) != null)
                        {
                            ModelState.AddModelError("Name", Localize("The page name you entered already exists."));
                            return View(model);
                        }

                        originalNavigation = page.Navigation; //So we can clear cache and this also indicates that we need to redirect to the new name.
                    }

                    page.ModifiedDate = DateTime.UtcNow;
                    page.ModifiedByUserId = SessionState.Profile.EnsureNotNull().UserId;
                    page.Body = model.Body ?? "";
                    page.Name = model.Name;
                    page.ChangeSummary = model.ChangeSummary ?? string.Empty;
                    page.Navigation = TwNamespaceNavigation.CleanAndValidate(model.Name);
                    page.Description = model.Description ?? "";

                    await pageRepository.UpsertPage(tightEngine, Localizer, page, SessionState);

                    await SessionState.SetPageId(page.Id);

                    model.SuccessMessage = Localize("The page was saved.");

                    if (string.IsNullOrWhiteSpace(originalNavigation) == false)
                    {
                        TwCache.ClearCategory(TwCacheKey.Build(TwCache.Category.Page, [originalNavigation]));
                        TwCache.ClearCategory(TwCacheKey.Build(TwCache.Category.Page, [page.Id]));
                        return Redirect($"{WikiConfiguration.BasePath}/{page.Navigation}/Edit");
                    }

                    return View(model);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while saving page with name {Name} and navigation {Navigation}.", model.Name, model.Navigation);
                throw;
            }
        }

        [Authorize]
        [HttpGet("Page/Template/{id:int}")]
        public async Task<ActionResult> Template(int id)
        {
            try
            {
                var template = await pageRepository.GetPageRevisionById(id);
                if (template == null)
                    return Json(new { body = "" });
                return Json(new { body = template.Body });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while loading page template with id {Id}.", id);
                throw;
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
        [HttpGet("Page/Image/{givenPageNavigation}/{givenFileNavigation}/{pageRevision:int?}")]
        public async Task<ActionResult> Image(string givenPageNavigation, string givenFileNavigation, int? pageRevision = null)
        {
            try
            {
                var pageNavigation = new TwNamespaceNavigation(givenPageNavigation);
                var fileNavigation = new TwNamespaceNavigation(givenFileNavigation);

                var scale = GetQueryValue<int?>("Scale");
                var maxWidth = GetQueryValue<int?>("MaxWidth");

                var cacheKey = TwCacheKeyFunction.Build(TwCache.Category.Page, [givenPageNavigation, givenFileNavigation, pageRevision, scale, maxWidth]);
                if (TwCache.TryGet<TwImageCacheItem>(cacheKey, out var cached))
                {
                    return File(cached.Bytes, cached.ContentType);
                }

                var file = await pageRepository.GetPageFileAttachmentByPageNavigationPageRevisionAndFileNavigation(pageNavigation.Canonical, fileNavigation.Canonical, pageRevision);

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
                            height += 16 - height;
                            width += 16 - height;
                        }
                        if (width < 16)
                        {
                            height += 16 - width;
                            width += 16 - width;
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

                        using var image = TwImages.ResizeImage(img, width, height);
                        using var ms = new MemoryStream();
                        image.SaveAsPng(ms);

                        var cacheItem = new TwImageCacheItem(ms.ToArray(), "image/png");
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
                Logger.LogError(ex, "An error occurred while loading an image with file navigation {FileNavigation} attached to page with navigation {PageNavigation} and page revision {PageRevision}.", givenFileNavigation, givenPageNavigation, pageRevision);
                throw;
            }
        }

        /// <summary>
        /// Gets an image from the database, converts it to a PNG with optional scaling and returns it to the client.
        /// </summary>
        /// <param name="givenPageNavigation">The navigation link of the page.</param>
        /// <param name="givenFileNavigation">The navigation link of the file.</param>
        /// <param name="pageRevision">The revision of the the PAGE that the file is attached to (NOT THE FILE REVISION)</param>
        [AllowAnonymous]
        [HttpGet("Page/Png/{givenPageNavigation}/{givenFileNavigation}/{pageRevision:int?}")]
        public async Task<ActionResult> Png(string givenPageNavigation, string givenFileNavigation, int? pageRevision = null)
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

                string givenScale = GetQueryValue("Scale", "100");

                var file = await pageRepository.GetPageFileAttachmentByPageNavigationPageRevisionAndFileNavigation(pageNavigation.Canonical, fileNavigation.Canonical, pageRevision);
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

                        using var image = TwImages.ResizeImage(img, width, height);
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
                    return NotFound(Localize("[{0}] was not found on the page [{1}].", fileNavigation, pageNavigation));
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while loading an image as PNG with file navigation {FileNavigation} attached to page with navigation {PageNavigation} and page revision {PageRevision}.", givenFileNavigation, givenPageNavigation, pageRevision);
                throw;
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
        public async Task<ActionResult> Binary(string givenPageNavigation, string givenFileNavigation, int? pageRevision = null)
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

                var file = await pageRepository.GetPageFileAttachmentByPageNavigationPageRevisionAndFileNavigation(pageNavigation.Canonical, fileNavigation.Canonical, pageRevision);

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
                Logger.LogError(ex, "An error occurred while loading a binary file with file navigation {FileNavigation} attached to page with navigation {PageNavigation} and page revision {PageRevision}.", givenFileNavigation, givenPageNavigation, pageRevision);
                throw;
            }
        }

        #endregion

        #region Export/import

        [Authorize]
        [HttpGet("{givenCanonical}/Export")]
        public async Task<ActionResult> Export(string givenCanonical)
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(givenCanonical, WikiPermission.Read);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                var navigation = new TwNamespaceNavigation(givenCanonical);

                var page = await pageRepository.GetPageRevisionByNavigation(navigation.Canonical);
                if (page == null)
                {
                    return NotFound();
                }

                var sr = new StringWriter();
                var writer = new System.Xml.XmlTextWriter(sr);
                var serializer = new XmlSerializer(typeof(TwPage));
                serializer.Serialize(writer, page);

                return File(Encoding.UTF8.GetBytes(sr.ToString()), "text/xml", $"{givenCanonical}.xml");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while exporting page with navigation {Navigation}.", givenCanonical);
                throw;
            }
        }

        #endregion
    }
}
