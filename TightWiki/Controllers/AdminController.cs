using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using NTDLS.DelegateThreadPooling;
using NTDLS.Helpers;
using System.Reflection;
using TightWiki.Caching;
using TightWiki.Engine.Implementation.Utility;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.Library;
using TightWiki.Models;
using TightWiki.Models.DataModels;
using TightWiki.Models.ViewModels.Admin;
using TightWiki.Models.ViewModels.Page;
using TightWiki.Models.ViewModels.Utility;
using TightWiki.Repository;
using static TightWiki.Library.Constants;

namespace TightWiki.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class AdminController(
        ITightEngine tightEngine,
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IStringLocalizer<AdminController> localizer)
        : WikiControllerBase<AdminController>(signInManager, userManager, localizer)
    {
        #region Metrics.

        [Authorize]
        [HttpGet("Database")]
        public ActionResult Database()
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            SessionState.Page.Name = Localize("Database");

            var versions = SpannedRepository.GetDatabaseVersions();
            var pageCounts = SpannedRepository.GetDatabasePageCounts();
            var pageSizes = SpannedRepository.GetDatabasePageSizes();

            var info = new List<DatabaseInfo>();

            foreach (var version in versions)
            {
                var pageCount = pageCounts.FirstOrDefault(o => o.Name == version.Name).PageCount;
                var pageSize = pageSizes.FirstOrDefault(o => o.Name == version.Name).PageSize;

                info.Add(new DatabaseInfo
                {
                    Name = version.Name,
                    Version = version.Version,
                    PageCount = pageCount,
                    PageSize = pageSize,
                    DatabaseSize = pageCount * pageSize
                });
            }

            var model = new DatabaseViewModel()
            {
                Info = info
            };

            return View(model);
        }

        [Authorize]
        [HttpPost("Database/{databaseAction}/{database}")]
        public ActionResult Database(ConfirmActionViewModel model, string databaseAction, string database)
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            SessionState.Page.Name = Localize("Database");

            if (model.UserSelection == true)
            {
                try
                {
                    switch (databaseAction)
                    {
                        case "Optimize":
                            {
                                var resultText = SpannedRepository.OptimizeDatabase(database);
                                return NotifyOfSuccess(Localize("Optimization complete. {0}", resultText), model.YesRedirectURL);
                            }
                        case "Vacuum":
                            {
                                var resultText = SpannedRepository.OptimizeDatabase(database);
                                return NotifyOfSuccess(Localize("Vacuum complete. {0}", resultText), model.YesRedirectURL);
                            }
                        case "Verify":
                            {
                                var resultText = SpannedRepository.OptimizeDatabase(database);
                                return NotifyOfSuccess(Localize("Verification complete. {0}", resultText), model.YesRedirectURL);
                            }
                    }
                }
                catch (Exception ex)
                {
                    return NotifyOfError(Localize("Operation failed: {0}", ex.Message), model.YesRedirectURL);
                }

                return NotifyOfError(Localize("Unknown database action: '{0}'", databaseAction), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        #endregion

        #region Metrics.

        [Authorize]
        [HttpGet("Metrics")]
        public ActionResult Metrics()
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            SessionState.Page.Name = Localize("Metrics");

            var version = string.Join('.', (Assembly.GetExecutingAssembly()
                .GetName().Version?.ToString() ?? "0.0.0.0").Split('.').Take(3)); //Major.Minor.Patch

            var model = new MetricsViewModel()
            {
                Metrics = ConfigurationRepository.GetWikiDatabaseMetrics(),
                ApplicationVersion = version
            };

            return View(model);
        }

        [Authorize]
        [HttpPost("PurgeCompilationStatistics")]
        public ActionResult PurgeCompilationStatistics(ConfirmActionViewModel model)
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (model.UserSelection == true)
            {
                StatisticsRepository.PurgeCompilationStatistics();
                return NotifyOfSuccess(Localize("Compilation statistics purged."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        [Authorize]
        [HttpPost("PurgeMemoryCache")]
        public ActionResult PurgeMemoryCache(ConfirmActionViewModel model)
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (model.UserSelection == true)
            {
                WikiCache.Clear();
                return NotifyOfSuccess(Localize("Memory cache purged."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        #endregion

        #region Compilation Statistics.

        [Authorize]
        [HttpGet("CompilationStatistics")]
        public ActionResult CompilationStatistics()
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            SessionState.Page.Name = Localize("Compilations Statistics");

            var pageNumber = GetQueryValue("page", 1);
            var orderBy = GetQueryValue("OrderBy");
            var orderByDirection = GetQueryValue("OrderByDirection");

            var model = new PageCompilationStatisticsViewModel()
            {
                Statistics = StatisticsRepository.GetCompilationStatisticsPaged(pageNumber, orderBy, orderByDirection),
            };

            model.PaginationPageCount = (model.Statistics.FirstOrDefault()?.PaginationPageCount ?? 0);

            model.Statistics.ForEach(o =>
            {
                o.LatestBuild = SessionState.LocalizeDateTime(o.LatestBuild);
            });

            return View(model);
        }

        #endregion

        #region Moderate.

        [Authorize]
        [HttpGet("Moderate")]
        public ActionResult Moderate()
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }

            SessionState.Page.Name = Localize("Page Moderation");

            var instruction = GetQueryValue("Instruction");
            if (instruction != null)
            {
                var model = new PageModerateViewModel()
                {
                    Pages = PageRepository.GetAllPagesByInstructionPaged(GetQueryValue("page", 1), instruction),
                    Instruction = instruction,
                    Instructions = typeof(WikiInstruction).GetProperties().Select(o => o.Name).ToList()
                };

                model.PaginationPageCount = (model.Pages.FirstOrDefault()?.PaginationPageCount ?? 0);

                if (model.Pages != null && model.Pages.Count > 0)
                {
                    model.Pages.ForEach(o =>
                    {
                        o.CreatedDate = SessionState.LocalizeDateTime(o.CreatedDate);
                        o.ModifiedDate = SessionState.LocalizeDateTime(o.ModifiedDate);
                    });
                }

                return View(model);
            }

            return View(new PageModerateViewModel()
            {
                Pages = new(),
                Instruction = string.Empty,
                Instructions = typeof(WikiInstruction).GetProperties().Select(o => o.Name).ToList()
            });
        }

        #endregion

        #region Missing Pages.

        [Authorize]
        [HttpGet("MissingPages")]
        public ActionResult MissingPages()
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            SessionState.Page.Name = Localize("Missing Pages");

            var pageNumber = GetQueryValue("page", 1);
            var orderBy = GetQueryValue("OrderBy");
            var orderByDirection = GetQueryValue("OrderByDirection");

            var model = new MissingPagesViewModel()
            {
                Pages = PageRepository.GetMissingPagesPaged(pageNumber, orderBy, orderByDirection)
            };

            model.PaginationPageCount = (model.Pages.FirstOrDefault()?.PaginationPageCount ?? 0);

            return View(model);
        }

        #endregion

        #region Namespaces.

        [Authorize]
        [HttpGet("Namespaces")]
        public ActionResult Namespaces()
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            SessionState.Page.Name = Localize("Namespaces");

            var pageNumber = GetQueryValue("page", 1);
            var orderBy = GetQueryValue("OrderBy");
            var orderByDirection = GetQueryValue("OrderByDirection");

            var model = new NamespacesViewModel()
            {
                Namespaces = PageRepository.GetAllNamespacesPaged(pageNumber, orderBy, orderByDirection),
            };

            model.PaginationPageCount = (model.Namespaces.FirstOrDefault()?.PaginationPageCount ?? 0);

            return View(model);
        }

        [Authorize]
        [HttpGet("Namespace/{namespaceName?}")]
        public ActionResult Namespace(string? namespaceName = null)
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            SessionState.Page.Name = Localize("Namespace");

            var pageNumber = GetQueryValue("page", 1);
            var orderBy = GetQueryValue("OrderBy");
            var orderByDirection = GetQueryValue("OrderByDirection");

            var model = new NamespaceViewModel()
            {
                Pages = PageRepository.GetAllNamespacePagesPaged(pageNumber, namespaceName ?? string.Empty, orderBy, orderByDirection),
                Namespace = namespaceName ?? string.Empty
            };

            model.PaginationPageCount = (model.Pages.FirstOrDefault()?.PaginationPageCount ?? 0);

            if (model.Pages != null && model.Pages.Count > 0)
            {
                model.Pages.ForEach(o =>
                {
                    o.CreatedDate = SessionState.LocalizeDateTime(o.CreatedDate);
                    o.ModifiedDate = SessionState.LocalizeDateTime(o.ModifiedDate);
                });
            }

            return View(model);
        }

        #endregion

        #region Pages.

        [Authorize]
        [HttpGet("Pages")]
        public ActionResult Pages()
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            SessionState.Page.Name = Localize("Pages");

            var searchString = GetQueryValue("SearchString");
            var orderBy = GetQueryValue("OrderBy");
            var orderByDirection = GetQueryValue("OrderByDirection");

            var model = new PagesViewModel()
            {
                Pages = PageRepository.GetAllPagesPaged(GetQueryValue("page", 1), orderBy, orderByDirection, Utility.SplitToTokens(searchString)),
                SearchString = searchString ?? string.Empty
            };

            model.PaginationPageCount = (model.Pages.FirstOrDefault()?.PaginationPageCount ?? 0);

            if (model.Pages != null && model.Pages.Count > 0)
            {
                model.Pages.ForEach(o =>
                {
                    o.CreatedDate = SessionState.LocalizeDateTime(o.CreatedDate);
                    o.ModifiedDate = SessionState.LocalizeDateTime(o.ModifiedDate);
                });
            }

            return View(model);
        }

        #endregion

        #region Revisions.

        [Authorize]
        [HttpPost("RevertPageRevision/{givenCanonical}/{revision:int}")]
        public ActionResult Revert(string givenCanonical, int revision, ConfirmActionViewModel model)
        {
            try
            {
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            SessionState.RequirePermission(null, WikiPermission.Moderate);

            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);

            if (model.UserSelection == true)
            {
                var page = PageRepository.GetPageRevisionByNavigation(pageNavigation, revision).EnsureNotNull();

                int currentPageRevision = PageRepository.GetCurrentPageRevision(page.Id);
                if (revision >= currentPageRevision)
                {
                    return NotifyOfError(Localize("You cannot revert to the current page revision."));
                }

                Engine.Implementation.Helpers.UpsertPage(tightEngine, page, SessionState);

                return NotifyOfSuccess(Localize("The page has been reverted."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        [Authorize]
        [HttpGet("DeletedPageRevisions/{pageId:int}")]
        public ActionResult DeletedPageRevisions(int pageId)
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            var pageNumber = GetQueryValue("page", 1);
            var orderBy = GetQueryValue("OrderBy");
            var orderByDirection = GetQueryValue("OrderByDirection");

            var model = new DeletedPagesRevisionsViewModel()
            {
                Revisions = PageRepository.GetDeletedPageRevisionsByIdPaged(pageId, pageNumber, orderBy, orderByDirection)
            };

            var page = PageRepository.GetLimitedPageInfoByIdAndRevision(pageId);
            if (page == null)
            {
                return NotifyOfError(Localize("The specified page could not be found."));
            }

            model.Name = page.Name;
            model.Namespace = page.Namespace;
            model.Navigation = page.Navigation;
            model.PageId = pageId;
            model.PaginationPageCount = (model.Revisions.FirstOrDefault()?.PaginationPageCount ?? 0);

            model.Revisions.ForEach(o =>
            {
                o.DeletedDate = SessionState.LocalizeDateTime(o.DeletedDate);
            });

            return View(model);
        }

        [Authorize]
        [HttpGet("DeletedPageRevision/{pageId:int}/{revision:int}")]
        public ActionResult DeletedPageRevision(int pageId, int revision)
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            var model = new DeletedPageRevisionViewModel();

            var page = PageRepository.GetDeletedPageRevisionById(pageId, revision);

            if (page != null)
            {
                var state = tightEngine.Transform(SessionState, page);
                model.PageId = pageId;
                model.Revision = pageId;
                model.Body = state.HtmlResult;
                model.DeletedDate = SessionState.LocalizeDateTime(page.DeletedDate);
                model.DeletedByUserName = page.DeletedByUserName;
            }

            return View(model);
        }

        [Authorize]
        [HttpGet("PageRevisions/{givenCanonical}")]
        public ActionResult PageRevisions(string givenCanonical)
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);

            var pageNumber = GetQueryValue("page", 1);
            var orderBy = GetQueryValue("OrderBy");
            var orderByDirection = GetQueryValue("OrderByDirection");

            var model = new PageRevisionsViewModel()
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
                p.ChangeAnalysis = Differentiator.GetComparisonSummary(thisRev?.Body ?? "", prevRev?.Body ?? "");
            }

            if (model.Revisions != null && model.Revisions.Count > 0)
            {
                SessionState.SetPageId(model.Revisions.First().PageId);
            }

            return View(model);
        }

        [Authorize]
        [HttpPost("DeletePageRevision/{givenCanonical}/{revision:int}")]
        public ActionResult DeletePageRevision(ConfirmActionViewModel model, string givenCanonical, int revision)
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);

            if (model.UserSelection == true)
            {
                var page = PageRepository.GetPageInfoByNavigation(pageNavigation);
                if (page == null)
                {
                    return NotifyOfError(Localize("The page could not be found."));
                }

                int revisionCount = PageRepository.GetPageRevisionCountByPageId(page.Id);
                if (revisionCount <= 1)
                {
                    return NotifyOfError(Localize("You cannot delete the only existing revision of a page, instead you would need to delete the entire page."));
                }

                //If we are deleting the latest revision, then we need to grab the previous
                //  version and make it the latest then delete the specified revision.
                if (revision >= page.Revision)
                {
                    int previousRevision = PageRepository.GetPagePreviousRevision(page.Id, revision);
                    var previousPageRevision = PageRepository.GetPageRevisionByNavigation(pageNavigation, previousRevision).EnsureNotNull();
                    Engine.Implementation.Helpers.UpsertPage(tightEngine, previousPageRevision, SessionState);
                }

                PageRepository.MovePageRevisionToDeletedById(page.Id, revision, SessionState.Profile.EnsureNotNull().UserId);

                return NotifyOfSuccess(Localize("Page revision has been moved to the deletion queue."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        #endregion

        #region Deleted Pages.

        [Authorize]
        [HttpGet("DeletedPage/{pageId}")]
        public ActionResult DeletedPage(int pageId)
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            var model = new DeletedPageViewModel();

            var page = PageRepository.GetDeletedPageById(pageId);

            if (page != null)
            {
                var state = tightEngine.Transform(SessionState, page);
                model.PageId = pageId;
                model.Body = state.HtmlResult;
                model.DeletedDate = SessionState.LocalizeDateTime(page.ModifiedDate);
                model.DeletedByUserName = page.DeletedByUserName;
            }

            return View(model);
        }

        [Authorize]
        [HttpGet("DeletedPages")]
        public ActionResult DeletedPages()
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            var searchString = GetQueryValue("SearchString", string.Empty);
            var pageNumber = GetQueryValue("page", 1);
            var orderBy = GetQueryValue("OrderBy");
            var orderByDirection = GetQueryValue("OrderByDirection");

            var model = new DeletedPagesViewModel()
            {
                Pages = PageRepository.GetAllDeletedPagesPaged(pageNumber, orderBy, orderByDirection, Utility.SplitToTokens(searchString)),
                SearchString = searchString
            };

            model.PaginationPageCount = (model.Pages.FirstOrDefault()?.PaginationPageCount ?? 0);

            return View(model);
        }

        [Authorize]
        [HttpPost("RebuildAllPages")]
        public ActionResult RebuildAllPages(ConfirmActionViewModel model)
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (model.UserSelection == true)
            {
                foreach (var page in PageRepository.GetAllPages())
                {
                    Engine.Implementation.Helpers.RefreshPageMetadata(tightEngine, page, SessionState);
                }
                return NotifyOfSuccess(Localize("All pages have been rebuilt."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        [Authorize]
        [HttpPost("PreCacheAllPages")]
        public ActionResult PreCacheAllPages(ConfirmActionViewModel model)
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            var pool = new DelegateThreadPool();

            if (model.UserSelection == true)
            {
                var workload = pool.CreateChildPool();

                foreach (var page in PageRepository.GetAllPages())
                {
                    workload.Enqueue(() =>
                    {
                        string queryKey = string.Empty;
                        foreach (var query in Request.Query)
                        {
                            queryKey += $"{query.Key}:{query.Value}";
                        }

                        var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Page, [page.Navigation, page.Revision, queryKey]);
                        if (WikiCache.Contains(cacheKey) == false)
                        {
                            var state = tightEngine.Transform(SessionState, page, page.Revision);
                            page.Body = state.HtmlResult;

                            if (state.ProcessingInstructions.Contains(WikiInstruction.NoCache) == false)
                            {
                                WikiCache.Put(cacheKey, state.HtmlResult); //This is cleared with the call to Cache.ClearCategory($"Page:{page.Navigation}");
                            }
                        }
                    });
                }

                workload.WaitForCompletion();

                return NotifyOfSuccess(Localize("All pages have been cached."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        [Authorize]
        [HttpPost("TruncatePageRevisions")]
        public ActionResult TruncatePageRevisions(ConfirmActionViewModel model)
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (model.UserSelection == true)
            {
                PageRepository.TruncateAllPageRevisions("YES");
                WikiCache.Clear();
                return NotifyOfSuccess(Localize("All page revisions have been truncated."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        [Authorize]
        [HttpPost("PurgeDeletedPageRevisions/{pageId:int}")]
        public ActionResult PurgeDeletedPageRevisions(ConfirmActionViewModel model, int pageId)
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (model.UserSelection == true)
            {
                PageRepository.PurgeDeletedPageRevisionsByPageId(pageId);
                return NotifyOfSuccess(Localize("The page deletion queue has been purged."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        [Authorize]
        [HttpPost("PurgeDeletedPageRevision/{pageId:int}/{revision:int}")]
        public ActionResult PurgeDeletedPageRevision(ConfirmActionViewModel model, int pageId, int revision)
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (model.UserSelection == true)
            {
                PageRepository.PurgeDeletedPageRevisionByPageIdAndRevision(pageId, revision);
                return NotifyOfSuccess(Localize("The page revision has been purged from the deletion queue."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        [Authorize]
        [HttpPost("RestoreDeletedPageRevision/{pageId:int}/{revision:int}")]
        public ActionResult RestoreDeletedPageRevision(ConfirmActionViewModel model, int pageId, int revision)
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (model.UserSelection == true)
            {
                PageRepository.RestoreDeletedPageRevisionByPageIdAndRevision(pageId, revision);
                return NotifyOfSuccess(Localize("The page revision has been restored."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        [Authorize]
        [HttpPost("PurgeDeletedPages")]
        public ActionResult PurgeDeletedPages(ConfirmActionViewModel model)
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (model.UserSelection == true)
            {
                PageRepository.PurgeDeletedPages();
                return NotifyOfSuccess(Localize("The page deletion queue has been purged."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        [Authorize]
        [HttpPost("PurgeDeletedPage/{pageId:int}")]
        public ActionResult PurgeDeletedPage(ConfirmActionViewModel model, int pageId)
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (model.UserSelection == true)
            {
                PageRepository.PurgeDeletedPageByPageId(pageId);
                return NotifyOfSuccess(Localize("The page has been purged from the deletion queue."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        [Authorize]
        [HttpPost("DeletePage/{pageId:int}")]
        public ActionResult DeletePage(ConfirmActionViewModel model, int pageId)
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (model.UserSelection == true)
            {
                PageRepository.MovePageToDeletedById(pageId, SessionState.Profile.EnsureNotNull().UserId);
                return NotifyOfSuccess(Localize("The page has been moved to the deletion queue."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        [Authorize]
        [HttpPost("RestoreDeletedPage/{pageId:int}")]
        public ActionResult RestoreDeletedPage(ConfirmActionViewModel model, int pageId)
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (model.UserSelection == true)
            {
                PageRepository.RestoreDeletedPageByPageId(pageId);
                var page = PageRepository.GetLatestPageRevisionById(pageId);
                if (page != null)
                {
                    Engine.Implementation.Helpers.RefreshPageMetadata(tightEngine, page, SessionState);
                }
                return NotifyOfSuccess(Localize("The page has restored."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        #endregion

        #region Files.

        [Authorize]
        [HttpGet("OrphanedPageAttachments")]
        public ActionResult OrphanedPageAttachments()
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            SessionState.Page.Name = Localize("Orphaned Page Attachments");

            var pageNumber = GetQueryValue("page", 1);
            var orderBy = GetQueryValue("OrderBy");
            var orderByDirection = GetQueryValue("OrderByDirection");

            var model = new OrphanedPageAttachmentsViewModel()
            {
                Files = PageFileRepository.GetOrphanedPageAttachmentsPaged(pageNumber, orderBy, orderByDirection),
            };

            model.PaginationPageCount = (model.Files.FirstOrDefault()?.PaginationPageCount ?? 0);

            /* Localization:
            if (model.Files != null && model.Files.Count > 0)
            {
                model.Files.ForEach(o =>
                {
                    o.CreatedDate = SessionState.LocalizeDateTime(o.CreatedDate);
                    o.ModifiedDate = SessionState.LocalizeDateTime(o.ModifiedDate);
                });
            }
            */

            return View(model);
        }

        [Authorize]
        [HttpPost("PurgeOrphanedAttachments")]
        public ActionResult PurgeOrphanedAttachments(ConfirmActionViewModel model)
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (model.UserSelection == true)
            {
                PageFileRepository.PurgeOrphanedPageAttachments();
                return NotifyOfSuccess(Localize("All orphaned page attachments have been purged."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }


        [Authorize]
        [HttpPost("PurgeOrphanedAttachment/{pageFileId:int}/{revision:int}")]
        public ActionResult PurgeOrphanedAttachment(ConfirmActionViewModel model, int pageFileId, int revision)
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (model.UserSelection == true)
            {
                PageFileRepository.PurgeOrphanedPageAttachment(pageFileId, revision);
                return NotifyOfSuccess(Localize("The pages orphaned attachments have been purged."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        #endregion

        #region Menu Items.

        [Authorize]
        [HttpGet("MenuItems")]
        public ActionResult MenuItems()
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            //var pageNumber = GetQueryValue("page", 1);
            var orderBy = GetQueryValue("OrderBy");
            var orderByDirection = GetQueryValue("OrderByDirection");

            var model = new MenuItemsViewModel()
            {
                Items = ConfigurationRepository.GetAllMenuItems(orderBy, orderByDirection)
            };

            return View(model);
        }

        [Authorize]
        [HttpGet("MenuItem/{id:int?}")]
        public ActionResult MenuItem(int? id)
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            SessionState.Page.Name = Localize("Menu Item");

            if (id != null)
            {
                var menuItem = ConfigurationRepository.GetMenuItemById((int)id);
                return View(menuItem.ToViewModel());
            }
            else
            {
                var model = new MenuItemViewModel
                {
                    Link = "/"
                };
                return View(model);
            }

        }

        /// <summary>
        /// Save site menu item.
        /// </summary>
        [Authorize]
        [HttpPost("MenuItem/{id:int?}")]
        public ActionResult MenuItem(int? id, MenuItemViewModel model)
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (ConfigurationRepository.GetAllMenuItems().Where(o => o.Name.Equals(model.Name, StringComparison.InvariantCultureIgnoreCase) && o.Id != model.Id).Any())
            {
                ModelState.AddModelError("Name", Localize("The menu name '{0}' is already in use.", model.Name));
                return View(model);
            }

            if (id.DefaultWhenNull(0) == 0)
            {
                model.Id = ConfigurationRepository.InsertMenuItem(model.ToDataModel());
                ModelState.Clear();

                return NotifyOfSuccess(Localize("The menu item has been created."), $"/Admin/MenuItem/{model.Id}");
            }
            else
            {
                ConfigurationRepository.UpdateMenuItemById(model.ToDataModel());
            }

            model.SuccessMessage = Localize("The menu item has been saved.");
            return View(model);
        }

        [Authorize]
        [HttpPost("DeleteMenuItem/{id:int}")]
        public ActionResult DeleteRole(ConfirmActionViewModel model, int id)
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (model.UserSelection == true)
            {
                ConfigurationRepository.DeleteMenuItemById(id);
                return NotifyOfSuccess(Localize("The specified menu item has been deleted."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        #endregion

        #region Config.

        [Authorize]
        [HttpGet("Config")]
        public ActionResult Config()
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            var model = new ConfigurationViewModel()
            {
                Themes = ConfigurationRepository.GetAllThemes(),
                Roles = UsersRepository.GetAllRoles(),
                TimeZones = TimeZoneItem.GetAll(),
                Countries = CountryItem.GetAll(),
                Languages = LanguageItem.GetAll(),
                Nest = ConfigurationRepository.GetConfigurationNest()
            };
            return View(model);
        }

        [Authorize]
        [HttpPost("Config")]
        public ActionResult Config(ConfigurationViewModel model)
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                model = new ConfigurationViewModel()
                {
                    Themes = ConfigurationRepository.GetAllThemes(),
                    Roles = UsersRepository.GetAllRoles(),
                    TimeZones = TimeZoneItem.GetAll(),
                    Countries = CountryItem.GetAll(),
                    Languages = LanguageItem.GetAll(),
                    Nest = ConfigurationRepository.GetConfigurationNest(),
                };

                var flatConfig = ConfigurationRepository.GetFlatConfiguration();

                foreach (var fc in flatConfig)
                {
                    var parent = model.Nest.Single(o => o.Name == fc.GroupName);
                    var child = parent.Entries.Single(o => o.Name == fc.EntryName);

                    var value = GetFormValue($"{fc.GroupId}:{fc.EntryId}", string.Empty);

                    //We keep the value in model.Nest.Entries.Value so that the page will reflect the new settings after post.
                    child.Value = value;

                    if (fc.IsRequired && string.IsNullOrEmpty(value))
                    {
                        model.ErrorMessage = Localize("{0} : {1} is required.", fc.GroupName, fc.EntryName);
                        return View(model);
                    }

                    if ($"{fc.GroupName}:{fc.EntryName}" == "Customization:Theme")
                    {
                        //This is not 100% necessary, I just want to prevent the user from needing to refresh to view the new theme.
                        GlobalConfiguration.SystemTheme = ConfigurationRepository.GetAllThemes().Single(o => o.Name == value);
                        if (string.IsNullOrEmpty(SessionState.Profile?.Theme))
                        {
                            SessionState.UserTheme = GlobalConfiguration.SystemTheme;
                        }
                    }

                    if (fc.IsEncrypted)
                    {
                        value = Security.Helpers.EncryptString(Security.Helpers.MachineKey, value);
                    }

                    ConfigurationRepository.SaveConfigurationEntryValueByGroupAndEntry(fc.GroupName, fc.EntryName, value);
                }

                WikiCache.ClearCategory(WikiCache.Category.Configuration);

                model.SuccessMessage = Localize("The configuration has been saved successfully!");
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.Message);
            }

            return View(model);
        }

        #endregion

        #region Emojis.

        [Authorize]
        [HttpGet("Emojis")]
        public ActionResult Emojis()
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            SessionState.Page.Name = Localize("Emojis");

            var pageNumber = GetQueryValue("page", 1);
            var orderBy = GetQueryValue("OrderBy");
            var orderByDirection = GetQueryValue("OrderByDirection");
            var searchString = GetQueryValue("SearchString") ?? string.Empty;

            var model = new EmojisViewModel()
            {
                Emojis = EmojiRepository.GetAllEmojisPaged(pageNumber, orderBy, orderByDirection, Utility.SplitToTokens(searchString)),
                SearchString = searchString
            };

            model.PaginationPageCount = (model.Emojis.FirstOrDefault()?.PaginationPageCount ?? 0);

            return View(model);
        }

        [Authorize]
        [HttpGet("Emoji/{name}")]
        public ActionResult Emoji(string name)
        {
            try
            {
                SessionState.RequirePermission(null, WikiPermission.Moderate);
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            var emoji = EmojiRepository.GetEmojiByName(name);

            var model = new EmojiViewModel
            {
                Emoji = emoji ?? new Emoji(),
                Categories = string.Join(",", EmojiRepository.GetEmojiCategoriesByName(name).Select(o => o.Category).ToList()),
                OriginalName = emoji?.Name ?? string.Empty
            };

            return View(model);
        }

        /// <summary>
        /// Update an existing emoji.
        /// </summary>
        [Authorize]
        [HttpPost("Emoji/{name}")]
        public ActionResult Emoji(EmojiViewModel model)
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            bool nameChanged = false;

            if (!model.OriginalName.Equals(model.Emoji.Name, StringComparison.InvariantCultureIgnoreCase))
            {
                nameChanged = true;
                var checkName = EmojiRepository.GetEmojiByName(model.Emoji.Name.ToLowerInvariant());
                if (checkName != null)
                {
                    ModelState.AddModelError("Emoji.Name", Localize("Emoji name is already in use."));
                    return View(model);
                }
            }

            var emoji = new UpsertEmoji
            {
                Id = model.Emoji.Id,
                Name = model.Emoji.Name.ToLowerInvariant(),
                Categories = Utility.SplitToTokens($"{model.Categories} {model.Emoji.Name} {Text.SeparateCamelCase(model.Emoji.Name)}")
            };

            var file = Request.Form.Files["ImageData"];
            if (file != null && file.Length > 0)
            {
                if (file.Length > GlobalConfiguration.MaxEmojiFileSize)
                {
                    model.ErrorMessage += Localize("Could not save the attached image, too large.");
                }
                else
                {
                    try
                    {
                        emoji.ImageData = Utility.ConvertHttpFileToBytes(file);
                        _ = SixLabors.ImageSharp.Image.Load(new MemoryStream(emoji.ImageData));
                        emoji.MimeType = file.ContentType;
                    }
                    catch
                    {
                        model.ErrorMessage += Localize("Could not save the attached image.");
                    }
                }
            }

            emoji.Id = EmojiRepository.UpsertEmoji(emoji);
            model.OriginalName = model.Emoji.Name;
            model.SuccessMessage = Localize("The emoji has been saved successfully!");
            model.Emoji.Id = (int)emoji.Id;
            ModelState.Clear();

            ConfigurationRepository.ReloadEmojis();

            if (nameChanged)
            {
                return NotifyOfSuccess(Localize("The emoji has been saved."), $"/Admin/Emoji/{Navigation.Clean(emoji.Name)}");
            }

            return View(model);
        }

        [Authorize]
        [HttpGet("AddEmoji")]
        public ActionResult AddEmoji()
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            var model = new AddEmojiViewModel()
            {
                Name = string.Empty,
                OriginalName = string.Empty,
                Categories = string.Empty
            };

            return View(model);
        }

        /// <summary>
        /// Save user profile.
        /// </summary>
        [Authorize]
        [HttpPost("AddEmoji")]
        public ActionResult AddEmoji(AddEmojiViewModel model)
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (string.IsNullOrEmpty(model.OriginalName) == true || !model.OriginalName.Equals(model.Name, StringComparison.InvariantCultureIgnoreCase))
            {
                var checkName = EmojiRepository.GetEmojiByName(model.Name.ToLowerInvariant());
                if (checkName != null)
                {
                    ModelState.AddModelError("Name", Localize("Emoji name is already in use."));
                    return View(model);
                }
            }

            var emoji = new UpsertEmoji
            {
                Id = model.Id,
                Name = model.Name.ToLowerInvariant(),
                Categories = Utility.SplitToTokens($"{model.Categories} {model.Name} {Text.SeparateCamelCase(model.Name)}")
            };

            var file = Request.Form.Files["ImageData"];
            if (file != null && file.Length > 0)
            {
                if (file.Length > GlobalConfiguration.MaxEmojiFileSize)
                {
                    ModelState.AddModelError("Name", Localize("Could not save the attached image, too large."));
                }
                else
                {
                    try
                    {
                        emoji.ImageData = Utility.ConvertHttpFileToBytes(file);
                        var image = SixLabors.ImageSharp.Image.Load(new MemoryStream(emoji.ImageData));
                        emoji.MimeType = file.ContentType;
                    }
                    catch
                    {
                        ModelState.AddModelError("Name", Localize("Could not save the attached image."));
                    }
                }
            }

            EmojiRepository.UpsertEmoji(emoji);

            return NotifyOfSuccess(Localize("The emoji has been created."), $"/Admin/Emoji/{Navigation.Clean(emoji.Name)}");
        }

        [Authorize]
        [HttpPost("DeleteEmoji/{name}")]
        public ActionResult DeleteRole(ConfirmActionViewModel model, string name)
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            var emoji = EmojiRepository.GetEmojiByName(name);

            if (model.UserSelection == true && emoji != null)
            {
                EmojiRepository.DeleteById(emoji.Id);
                return NotifyOfSuccess(Localize("The specified emoji has been deleted."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        #endregion

        #region Exceptions.

        [Authorize]
        [HttpGet("Exceptions")]
        public ActionResult Exceptions()
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            SessionState.Page.Name = Localize("Exceptions");

            var pageNumber = GetQueryValue("page", 1);
            var orderBy = GetQueryValue("OrderBy");
            var orderByDirection = GetQueryValue("OrderByDirection");

            var model = new ExceptionsViewModel()
            {
                Exceptions = ExceptionRepository.GetAllExceptionsPaged(pageNumber, orderBy, orderByDirection)
            };

            model.PaginationPageCount = (model.Exceptions.FirstOrDefault()?.PaginationPageCount ?? 0);

            return View(model);
        }

        [Authorize]
        [HttpGet("Exception/{id}")]
        public ActionResult Exception(int id)
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            SessionState.Page.Name = Localize("Exception");

            var model = new ExceptionViewModel()
            {
                Exception = ExceptionRepository.GetExceptionById(id)
            };

            return View(model);
        }

        [Authorize]
        [HttpPost("PurgeExceptions")]
        public ActionResult PurgeExceptions(ConfirmActionViewModel model)
        {
            try
            {
                SessionState.RequireAdminPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            if (model.UserSelection == true)
            {
                ExceptionRepository.PurgeExceptions();
                return NotifyOfSuccess(Localize("All exceptions have been purged."), model.YesRedirectURL);
            }

            return Redirect($"{GlobalConfiguration.BasePath}{model.NoRedirectURL}");
        }

        #endregion
    }
}
