using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NTDLS.DelegateThreadPooling;
using NTDLS.Helpers;
using System.Reflection;
using TightWiki.Library;
using TightWiki.Library.Caching;
using TightWiki.Library.Security;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Library;
using TightWiki.Plugin.Models;
using TightWiki.ViewModels.Admin;
using TightWiki.ViewModels.Utility;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class AdminController(
            ILogger<ITwEngine> logger,
            ITwConfigurationRepository configurationRepository,
            ITwDatabaseManager databaseManager,
            ITwEmojiRepository emojiRepository,
            ITwEngine tightEngine,
            ITwLoggingRepository loggingRepository,
            ITwPageRepository pageRepository,
            ITwSharedLocalizationText localizer,
            ITwStatisticsRepository statisticsRepository,
            ITwUsersRepository usersRepository,
            SignInManager<IdentityUser> signInManager,
            TwConfiguration wikiConfiguration,
            Repository.Helpers.ConfigurationManager configurationManager,
            UserManager<IdentityUser> userManager
        )
        : TwController<AdminController>(logger, signInManager, userManager, localizer, wikiConfiguration, databaseManager)
    {
        #region Metrics.

        [Authorize]
        [HttpGet("Database")]
        public async Task<ActionResult> Database()
        {
            try
            {
                try
                {
                    await SessionState.RequireAdminPermission();
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                SessionState.Page.Name = Localize("Database");

                var versions = await DatabaseManager.GetDatabaseVersions();
                var pageCounts = await DatabaseManager.GetDatabasePageCounts();
                var pageSizes = await DatabaseManager.GetDatabasePageSizes();

                var info = new List<TwDatabaseInfo>();

                foreach (var version in versions)
                {
                    var pageCount = pageCounts.FirstOrDefault(o => o.Name == version.Name).PageCount;
                    var pageSize = pageSizes.FirstOrDefault(o => o.Name == version.Name).PageSize;

                    info.Add(new TwDatabaseInfo
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
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while retrieving database information.");
                throw;
            }
        }

        [Authorize]
        [HttpPost("Database/restore/{defaultData}")]
        public async Task<ActionResult> Database(ConfirmActionViewModel model, string defaultData)
        {
            try
            {
                var defaultDataType = Enum.Parse<WikiDefaultDataType>(defaultData);

                try
                {
                    await SessionState.RequireAdminPermission();
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                SessionState.Page.Name = Localize("Restore");

                if (model.UserSelection == true)
                {
                    try
                    {
                        await DatabaseManager.ApplyAllSeedData(Localizer, UserManager, tightEngine, [defaultDataType]);

                        return NotifyOfSuccess(Localize("Restore complete."), model.YesRedirectURL);

                    }
                    catch (Exception ex)
                    {
                        return NotifyOfError(Localize("Operation failed: {0}", ex.Message), model.YesRedirectURL);
                    }
                }

                return Redirect($"{WikiConfiguration.BasePath}{model.NoRedirectURL}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while restoring default data of type '{DefaultDataType}'.", defaultData);
                throw;
            }
        }

        [Authorize]
        [HttpPost("Database/{databaseAction}/{database}")]
        public async Task<ActionResult> Database(ConfirmActionViewModel model, string databaseAction, string database)
        {
            try
            {
                try
                {
                    await SessionState.RequireAdminPermission();
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
                                    var resultText = await DatabaseManager.OptimizeDatabase(database);
                                    return NotifyOfSuccess(Localize("Optimization complete. {0}", resultText), model.YesRedirectURL);
                                }
                            case "Vacuum":
                                {
                                    var resultText = await DatabaseManager.OptimizeDatabase(database);
                                    return NotifyOfSuccess(Localize("Vacuum complete. {0}", resultText), model.YesRedirectURL);
                                }
                            case "Verify":
                                {
                                    var resultText = await DatabaseManager.OptimizeDatabase(database);
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

                return Redirect($"{WikiConfiguration.BasePath}{model.NoRedirectURL}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while performing database action '{DatabaseAction}' on database '{Database}'.", databaseAction, database);
                throw;
            }
        }

        #endregion

        #region Metrics.

        [Authorize]
        [HttpGet("Metrics")]
        public async Task<ActionResult> Metrics()
        {
            try
            {
                try
                {
                    await SessionState.RequireAdminPermission();
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
                    Metrics = await configurationRepository.GetWikiDatabaseMetrics(),
                    ApplicationVersion = version,
                    Exceptions = await loggingRepository.GetExceptionCount()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while retrieving wiki metrics.");
                throw;
            }
        }

        [Authorize]
        [HttpPost("PurgePageStatistics")]
        public async Task<ActionResult> PurgePageStatistics(ConfirmActionViewModel model)
        {
            try
            {
                try
                {
                    await SessionState.RequireAdminPermission();
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                if (model.UserSelection == true)
                {
                    await statisticsRepository.PurgePageStatistics();
                    return NotifyOfSuccess(Localize("Page statistics purged."), model.YesRedirectURL);
                }

                return Redirect($"{WikiConfiguration.BasePath}{model.NoRedirectURL}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while purging page statistics.");
                throw;
            }
        }

        [Authorize]
        [HttpPost("PurgeMemoryCache")]
        public async Task<ActionResult> PurgeMemoryCache(ConfirmActionViewModel model)
        {
            try
            {
                try
                {
                    await SessionState.RequireAdminPermission();
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                if (model.UserSelection == true)
                {
                    MemCache.Clear();
                    return NotifyOfSuccess(Localize("Memory cache purged."), model.YesRedirectURL);
                }

                return Redirect($"{WikiConfiguration.BasePath}{model.NoRedirectURL}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while purging the memory cache.");
                throw;
            }
        }

        #endregion

        #region Page Statistics.

        [Authorize]
        [HttpGet("PageStatistics")]
        public async Task<ActionResult> PageStatistics()
        {
            try
            {
                try
                {
                    await SessionState.RequireAdminPermission();
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                SessionState.Page.Name = Localize("Page Statistics");

                var pageNumber = GetQueryValue("page", 1);
                var orderBy = GetQueryValue<string>("OrderBy");
                var orderByDirection = GetQueryValue<string>("OrderByDirection");

                var model = new PageStatisticsViewModel()
                {
                    Statistics = await statisticsRepository.GetPageStatisticsPaged(pageNumber, orderBy, orderByDirection),
                };

                model.PaginationPageCount = (model.Statistics.FirstOrDefault()?.PaginationPageCount ?? 0);

                model.Statistics.ForEach(o =>
                {
                    o.LastCompileDateTime = SessionState.LocalizeDateTime(o.LastCompileDateTime);
                });

                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while retrieving page statistics.");
                throw;
            }
        }

        #endregion

        #region Moderate.

        [Authorize]
        [HttpGet("Moderate")]
        public async Task<ActionResult> Moderate()
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(null, WikiPermission.Moderate);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }

                SessionState.Page.Name = Localize("Page Moderation");

                var instruction = GetQueryValue<string>("Instruction");
                if (instruction != null)
                {
                    var model = new PageModerateViewModel()
                    {
                        Pages = await pageRepository.GetAllPagesByInstructionPaged(GetQueryValue("page", 1), instruction),
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
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while retrieving pages for moderation");
                throw;
            }
        }

        #endregion

        #region Missing Pages.

        [Authorize]
        [HttpGet("MissingPages")]
        public async Task<ActionResult> MissingPages()
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(null, WikiPermission.Moderate);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                SessionState.Page.Name = Localize("Missing Pages");

                var pageNumber = GetQueryValue("page", 1);
                var orderBy = GetQueryValue<string>("OrderBy");
                var orderByDirection = GetQueryValue<string>("OrderByDirection");

                var model = new MissingPagesViewModel()
                {
                    Pages = await pageRepository.GetMissingPagesPaged(pageNumber, orderBy, orderByDirection)
                };

                model.PaginationPageCount = (model.Pages.FirstOrDefault()?.PaginationPageCount ?? 0);

                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while retrieving missing pages.");
                throw;
            }
        }

        #endregion

        #region Namespaces.

        [Authorize]
        [HttpGet("Namespaces")]
        public async Task<ActionResult> Namespaces()
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(null, WikiPermission.Moderate);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                SessionState.Page.Name = Localize("Namespaces");

                var pageNumber = GetQueryValue("page", 1);
                var orderBy = GetQueryValue<string>("OrderBy");
                var orderByDirection = GetQueryValue<string>("OrderByDirection");

                var model = new NamespacesViewModel()
                {
                    Namespaces = await pageRepository.GetAllNamespacesPaged(pageNumber, orderBy, orderByDirection),
                };

                model.PaginationPageCount = (model.Namespaces.FirstOrDefault()?.PaginationPageCount ?? 0);

                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while retrieving namespaces.");
                throw;
            }
        }

        [Authorize]
        [HttpGet("Namespace/{namespaceName?}")]
        public async Task<ActionResult> Namespace(string? namespaceName = null)
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(null, WikiPermission.Moderate);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                SessionState.Page.Name = Localize("Namespace");

                var pageNumber = GetQueryValue("page", 1);
                var orderBy = GetQueryValue<string>("OrderBy");
                var orderByDirection = GetQueryValue<string>("OrderByDirection");

                var model = new NamespaceViewModel()
                {
                    Pages = await pageRepository.GetAllNamespacePagesPaged(pageNumber, namespaceName ?? string.Empty, orderBy, orderByDirection),
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
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while retrieving pages for namespace '{NamespaceName}'.", namespaceName);
                throw;
            }
        }

        #endregion

        #region Pages.

        [Authorize]
        [HttpGet("Pages")]
        public async Task<ActionResult> Pages()
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(null, WikiPermission.Moderate);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                SessionState.Page.Name = Localize("Pages");

                var searchString = GetQueryValue<string>("SearchString");
                var orderBy = GetQueryValue<string>("OrderBy");
                var orderByDirection = GetQueryValue<string>("OrderByDirection");

                var model = new PagesViewModel()
                {
                    Pages = await pageRepository.GetAllPagesPaged(GetQueryValue("page", 1), orderBy, orderByDirection, Utility.SplitToTokens(searchString)),
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
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while retrieving pages.");
                throw;
            }
        }

        #endregion

        #region Revisions.

        [Authorize]
        [HttpPost("RevertPageRevision/{givenCanonical}/{revision:int}")]
        public async Task<ActionResult> Revert(string givenCanonical, int revision, ConfirmActionViewModel model)
        {
            try
            {
                try
                {
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                await SessionState.RequirePermission(null, WikiPermission.Moderate);

                var pageNavigation = TwNamespaceNavigation.CleanAndValidate(givenCanonical);

                if (model.UserSelection == true)
                {
                    var page = (await pageRepository.GetPageRevisionByNavigation(pageNavigation, revision)).EnsureNotNull();

                    int currentPageRevision = await pageRepository.GetCurrentPageRevision(page.Id);
                    if (revision >= currentPageRevision)
                    {
                        return NotifyOfError(Localize("You cannot revert to the current page revision."));
                    }

                    await pageRepository.UpsertPage(tightEngine, Localizer, page, SessionState);

                    return NotifyOfSuccess(Localize("The page has been reverted."), model.YesRedirectURL);
                }

                return Redirect($"{WikiConfiguration.BasePath}{model.NoRedirectURL}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while reverting page '{GivenCanonical}' to revision '{Revision}'.", givenCanonical, revision);
                throw;
            }
        }

        [Authorize]
        [HttpGet("DeletedPageRevisions/{pageId:int}")]
        public async Task<ActionResult> DeletedPageRevisions(int pageId)
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(null, WikiPermission.Moderate);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                var pageNumber = GetQueryValue("page", 1);
                var orderBy = GetQueryValue<string>("OrderBy");
                var orderByDirection = GetQueryValue<string>("OrderByDirection");

                var model = new DeletedPagesRevisionsViewModel()
                {
                    Revisions = await pageRepository.GetDeletedPageRevisionsByIdPaged(pageId, pageNumber, orderBy, orderByDirection)
                };

                var page = await pageRepository.GetLimitedPageInfoByIdAndRevision(pageId);
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
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while retrieving deleted revisions for page with ID '{PageId}'.", pageId);
                throw;
            }
        }

        [Authorize]
        [HttpGet("DeletedPageRevision/{pageId:int}/{revision:int}")]
        public async Task<ActionResult> DeletedPageRevision(int pageId, int revision)
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(null, WikiPermission.Moderate);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                var model = new DeletedPageRevisionViewModel();

                var page = await pageRepository.GetDeletedPageRevisionById(pageId, revision);

                if (page != null)
                {
                    var state = await tightEngine.Transform(Localizer, SessionState, page);
                    model.PageId = pageId;
                    model.Revision = pageId;
                    model.Body = state.HtmlResult;
                    model.DeletedDate = SessionState.LocalizeDateTime(page.DeletedDate);
                    model.DeletedByUserName = page.DeletedByUserName;
                }

                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while retrieving deleted page revision for page with ID '{PageId}' and revision '{Revision}'.", pageId, revision);
                throw;
            }
        }

        [Authorize]
        [HttpGet("PageRevisions/{givenCanonical}")]
        public async Task<ActionResult> PageRevisions(string givenCanonical)
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(null, WikiPermission.Moderate);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                var pageNavigation = TwNamespaceNavigation.CleanAndValidate(givenCanonical);

                var pageNumber = GetQueryValue("page", 1);
                var orderBy = GetQueryValue<string>("OrderBy");
                var orderByDirection = GetQueryValue<string>("OrderByDirection");

                var model = new PageRevisionsViewModel()
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
                Logger.LogError(ex, "An error occurred while retrieving revisions for page with canonical navigation '{GivenCanonical}'.", givenCanonical);
                throw;
            }
        }

        [Authorize]
        [HttpPost("DeletePageRevision/{givenCanonical}/{revision:int}")]
        public async Task<ActionResult> DeletePageRevision(ConfirmActionViewModel model, string givenCanonical, int revision)
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(null, WikiPermission.Moderate);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                var pageNavigation = TwNamespaceNavigation.CleanAndValidate(givenCanonical);

                if (model.UserSelection == true)
                {
                    var page = await pageRepository.GetPageInfoByNavigation(pageNavigation);
                    if (page == null)
                    {
                        return NotifyOfError(Localize("The page could not be found."));
                    }

                    int revisionCount = await pageRepository.GetPageRevisionCountByPageId(page.Id);
                    if (revisionCount <= 1)
                    {
                        return NotifyOfError(Localize("You cannot delete the only existing revision of a page, instead you would need to delete the entire page."));
                    }

                    //If we are deleting the latest revision, then we need to grab the previous
                    //  version and make it the latest then delete the specified revision.
                    if (revision >= page.Revision)
                    {
                        int previousRevision = await pageRepository.GetPagePreviousRevision(page.Id, revision);
                        var previousPageRevision = await pageRepository.GetPageRevisionByNavigation(pageNavigation, previousRevision);
                        await pageRepository.UpsertPage(tightEngine, Localizer, previousPageRevision.EnsureNotNull(), SessionState);
                    }

                    await pageRepository.MovePageRevisionToDeletedById(page.Id, revision, SessionState.Profile.EnsureNotNull().UserId);

                    return NotifyOfSuccess(Localize("Page revision has been moved to the deletion queue."), model.YesRedirectURL);
                }

                return Redirect($"{WikiConfiguration.BasePath}{model.NoRedirectURL}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while deleting page revision for page with canonical navigation '{GivenCanonical}' and revision '{Revision}'.", givenCanonical, revision);
                throw;
            }
        }

        #endregion

        #region Deleted Pages.

        [Authorize]
        [HttpGet("DeletedPage/{pageId}")]
        public async Task<ActionResult> DeletedPage(int pageId)
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(null, WikiPermission.Moderate);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                var model = new DeletedPageViewModel();

                var page = await pageRepository.GetDeletedPageById(pageId);

                if (page != null)
                {
                    var state = await tightEngine.Transform(Localizer, SessionState, page);
                    model.PageId = pageId;
                    model.Body = state.HtmlResult;
                    model.DeletedDate = SessionState.LocalizeDateTime(page.ModifiedDate);
                    model.DeletedByUserName = page.DeletedByUserName;
                }

                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while retrieving deleted page with ID '{PageId}'.", pageId);
                throw;
            }
        }

        [Authorize]
        [HttpGet("DeletedPages")]
        public async Task<ActionResult> DeletedPages()
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(null, WikiPermission.Moderate);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                var searchString = GetQueryValue("SearchString", string.Empty);
                var pageNumber = GetQueryValue("page", 1);
                var orderBy = GetQueryValue<string>("OrderBy");
                var orderByDirection = GetQueryValue<string>("OrderByDirection");

                var model = new DeletedPagesViewModel()
                {
                    Pages = await pageRepository.GetAllDeletedPagesPaged(pageNumber, orderBy, orderByDirection, Utility.SplitToTokens(searchString)),
                    SearchString = searchString
                };

                model.PaginationPageCount = (model.Pages.FirstOrDefault()?.PaginationPageCount ?? 0);

                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while retrieving deleted pages.");
                throw;
            }
        }

        [Authorize]
        [HttpPost("RebuildAllPages")]
        public async Task<ActionResult> RebuildAllPages(ConfirmActionViewModel model)
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(null, WikiPermission.Moderate);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                if (model.UserSelection == true)
                {
                    foreach (var page in await pageRepository.GetAllPages())
                    {
                        await pageRepository.RefreshPageMetadata(tightEngine, Localizer, page, SessionState);
                    }
                    return NotifyOfSuccess(Localize("All pages have been rebuilt."), model.YesRedirectURL);
                }

                return Redirect($"{WikiConfiguration.BasePath}{model.NoRedirectURL}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while rebuilding all pages.");
                throw;
            }
        }

        [Authorize]
        [HttpPost("PreCacheAllPages")]
        public async Task<ActionResult> PreCacheAllPages(ConfirmActionViewModel model)
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(null, WikiPermission.Moderate);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                var pool = new DelegateThreadPool();

                if (model.UserSelection == true)
                {
                    var workload = pool.CreateChildPool();

                    //TODO: Should probably be a Paralell.ForEach().
                    foreach (var page in await pageRepository.GetAllPages())
                    {
                        workload.Enqueue(async () =>
                        {
                            string queryKey = string.Empty;
                            foreach (var query in Request.Query)
                            {
                                queryKey += $"{query.Key}:{query.Value}";
                            }

                            var cacheKey = MemCacheKeyFunction.Build(MemCache.Category.Page, [page.Navigation, page.Revision, queryKey]);
                            if (MemCache.Contains(cacheKey) == false)
                            {
                                var state = await tightEngine.Transform(Localizer, SessionState, page, page.Revision);
                                page.Body = state.HtmlResult;

                                if (state.ProcessingInstructions.Contains(WikiInstruction.NoCache) == false)
                                {
                                    MemCache.Set(cacheKey, state.HtmlResult); //This is cleared with the call to Cache.ClearCategory($"Page:{page.Navigation}");
                                }
                            }
                        });
                    }

                    workload.WaitForCompletion();

                    return NotifyOfSuccess(Localize("All pages have been cached."), model.YesRedirectURL);
                }

                return Redirect($"{WikiConfiguration.BasePath}{model.NoRedirectURL}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while pre-caching all pages.");
                throw;
            }
        }

        [Authorize]
        [HttpPost("TruncatePageRevisions")]
        public async Task<ActionResult> TruncatePageRevisions(ConfirmActionViewModel model)
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(null, WikiPermission.Moderate);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                if (model.UserSelection == true)
                {
                    await pageRepository.TruncateAllPageRevisions("YES");
                    MemCache.Clear();
                    return NotifyOfSuccess(Localize("All page revisions have been truncated."), model.YesRedirectURL);
                }

                return Redirect($"{WikiConfiguration.BasePath}{model.NoRedirectURL}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while truncating all page revisions.");
                throw;
            }
        }

        [Authorize]
        [HttpPost("PurgeDeletedPageRevisions/{pageId:int}")]
        public async Task<ActionResult> PurgeDeletedPageRevisions(ConfirmActionViewModel model, int pageId)
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(null, WikiPermission.Moderate);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                if (model.UserSelection == true)
                {
                    await pageRepository.PurgeDeletedPageRevisionsByPageId(pageId);
                    return NotifyOfSuccess(Localize("The page deletion queue has been purged."), model.YesRedirectURL);
                }

                return Redirect($"{WikiConfiguration.BasePath}{model.NoRedirectURL}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while purging deleted revisions for page with ID '{PageId}'.", pageId);
                throw;
            }
        }

        [Authorize]
        [HttpPost("PurgeDeletedPageRevision/{pageId:int}/{revision:int}")]
        public async Task<ActionResult> PurgeDeletedPageRevision(ConfirmActionViewModel model, int pageId, int revision)
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(null, WikiPermission.Moderate);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                if (model.UserSelection == true)
                {
                    await pageRepository.PurgeDeletedPageRevisionByPageIdAndRevision(pageId, revision);
                    return NotifyOfSuccess(Localize("The page revision has been purged from the deletion queue."), model.YesRedirectURL);
                }

                return Redirect($"{WikiConfiguration.BasePath}{model.NoRedirectURL}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while purging deleted page revision for page with ID '{PageId}' and revision '{Revision}'.", pageId, revision);
                throw;
            }
        }

        [Authorize]
        [HttpPost("RestoreDeletedPageRevision/{pageId:int}/{revision:int}")]
        public async Task<ActionResult> RestoreDeletedPageRevision(ConfirmActionViewModel model, int pageId, int revision)
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(null, WikiPermission.Moderate);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                if (model.UserSelection == true)
                {
                    await pageRepository.RestoreDeletedPageRevisionByPageIdAndRevision(pageId, revision);
                    return NotifyOfSuccess(Localize("The page revision has been restored."), model.YesRedirectURL);
                }

                return Redirect($"{WikiConfiguration.BasePath}{model.NoRedirectURL}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while restoring deleted page revision for page with ID '{PageId}' and revision '{Revision}'.", pageId, revision);
                throw;
            }
        }

        [Authorize]
        [HttpPost("PurgeDeletedPages")]
        public async Task<ActionResult> PurgeDeletedPages(ConfirmActionViewModel model)
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(null, WikiPermission.Moderate);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                if (model.UserSelection == true)
                {
                    await pageRepository.PurgeDeletedPages();
                    return NotifyOfSuccess(Localize("The page deletion queue has been purged."), model.YesRedirectURL);
                }

                return Redirect($"{WikiConfiguration.BasePath}{model.NoRedirectURL}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while purging all deleted pages.");
                throw;
            }
        }

        [Authorize]
        [HttpPost("PurgeDeletedPage/{pageId:int}")]
        public async Task<ActionResult> PurgeDeletedPage(ConfirmActionViewModel model, int pageId)
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(null, WikiPermission.Moderate);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                if (model.UserSelection == true)
                {
                    await pageRepository.PurgeDeletedPageByPageId(pageId);
                    return NotifyOfSuccess(Localize("The page has been purged from the deletion queue."), model.YesRedirectURL);
                }

                return Redirect($"{WikiConfiguration.BasePath}{model.NoRedirectURL}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while purging deleted page with ID '{PageId}'.", pageId);
                throw;
            }
        }

        [Authorize]
        [HttpPost("DeletePage/{pageId:int}")]
        public async Task<ActionResult> DeletePage(ConfirmActionViewModel model, int pageId)
        {
            try
            {
                try
                {
                    await SessionState.RequireAdminPermission();
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                if (model.UserSelection == true)
                {
                    await pageRepository.MovePageToDeletedById(pageId, SessionState.Profile.EnsureNotNull().UserId);
                    return NotifyOfSuccess(Localize("The page has been moved to the deletion queue."), model.YesRedirectURL);
                }

                return Redirect($"{WikiConfiguration.BasePath}{model.NoRedirectURL}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while deleting page with ID '{PageId}'.", pageId);
                throw;
            }
        }

        [Authorize]
        [HttpPost("RestoreDeletedPage/{pageId:int}")]
        public async Task<ActionResult> RestoreDeletedPage(ConfirmActionViewModel model, int pageId)
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(null, WikiPermission.Moderate);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                if (model.UserSelection == true)
                {
                    await pageRepository.RestoreDeletedPageByPageId(pageId);
                    var page = await pageRepository.GetLatestPageRevisionById(pageId);
                    if (page != null)
                    {
                        await pageRepository.RefreshPageMetadata(tightEngine, Localizer, page, SessionState);
                    }
                    return NotifyOfSuccess(Localize("The page has restored."), model.YesRedirectURL);
                }

                return Redirect($"{WikiConfiguration.BasePath}{model.NoRedirectURL}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while restoring deleted page with ID '{PageId}'.", pageId);
                throw;
            }
        }

        #endregion

        #region Files.

        [Authorize]
        [HttpGet("OrphanedPageAttachments")]
        public async Task<ActionResult> OrphanedPageAttachments()
        {
            try
            {
                try
                {
                    await SessionState.RequireAdminPermission();
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                SessionState.Page.Name = Localize("Orphaned Page Attachments");

                var pageNumber = GetQueryValue("page", 1);
                var orderBy = GetQueryValue<string>("OrderBy");
                var orderByDirection = GetQueryValue<string>("OrderByDirection");

                var model = new OrphanedPageAttachmentsViewModel()
                {
                    Files = await pageRepository.GetOrphanedPageAttachmentsPaged(pageNumber, orderBy, orderByDirection),
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
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while retrieving orphaned page attachments.");
                throw;
            }
        }

        [Authorize]
        [HttpPost("PurgeOrphanedAttachments")]
        public async Task<ActionResult> PurgeOrphanedAttachments(ConfirmActionViewModel model)
        {
            try
            {
                try
                {
                    await SessionState.RequireAdminPermission();
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                if (model.UserSelection == true)
                {
                    await pageRepository.PurgeOrphanedPageAttachments();
                    return NotifyOfSuccess(Localize("All orphaned page attachments have been purged."), model.YesRedirectURL);
                }

                return Redirect($"{WikiConfiguration.BasePath}{model.NoRedirectURL}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while purging all orphaned page attachments.");
                throw;
            }
        }

        [Authorize]
        [HttpPost("PurgeOrphanedAttachment/{pageFileId:int}/{revision:int}")]
        public async Task<ActionResult> PurgeOrphanedAttachment(ConfirmActionViewModel model, int pageFileId, int revision)
        {
            try
            {
                try
                {
                    await SessionState.RequireAdminPermission();
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                if (model.UserSelection == true)
                {
                    await pageRepository.PurgeOrphanedPageAttachment(pageFileId, revision);
                    return NotifyOfSuccess(Localize("The pages orphaned attachments have been purged."), model.YesRedirectURL);
                }

                return Redirect($"{WikiConfiguration.BasePath}{model.NoRedirectURL}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while purging orphaned page attachment with page file ID '{PageFileId}' and revision '{Revision}'.", pageFileId, revision);
                throw;
            }
        }

        #endregion

        #region Menu Items.

        [Authorize]
        [HttpGet("MenuItems")]
        public async Task<ActionResult> MenuItems()
        {
            try
            {
                try
                {
                    await SessionState.RequireAdminPermission();
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                //var pageNumber = GetQueryValue("page", 1);
                var orderBy = GetQueryValue<string>("OrderBy");
                var orderByDirection = GetQueryValue<string>("OrderByDirection");

                var model = new MenuItemsViewModel()
                {
                    Items = await configurationRepository.GetAllMenuItems(orderBy, orderByDirection)
                };

                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while retrieving menu items.");
                throw;
            }
        }

        [Authorize]
        [HttpGet("MenuItem/{id:int?}")]
        public async Task<ActionResult> MenuItem(int? id)
        {
            try
            {
                try
                {
                    await SessionState.RequireAdminPermission();
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                SessionState.Page.Name = Localize("Menu Item");

                if (id != null)
                {
                    var model = await configurationRepository.GetMenuItemById((int)id);
                    var viewModel = new MenuItemViewModel
                    {
                        Name = model.Name,
                        Id = model.Id,
                        Link = model.Link,
                        Ordinal = model.Ordinal
                    };

                    return View(viewModel);
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
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while retrieving menu item with ID '{Id}'.", id);
                throw;
            }
        }

        /// <summary>
        /// Save site menu item.
        /// </summary>
        [Authorize]
        [HttpPost("MenuItem/{id:int?}")]
        public async Task<ActionResult> MenuItem(int? id, MenuItemViewModel model)
        {
            try
            {
                try
                {
                    await SessionState.RequireAdminPermission();
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                if ((await configurationRepository.GetAllMenuItems()).Where(o => o.Name.Equals(model.Name, StringComparison.InvariantCultureIgnoreCase) && o.Id != model.Id).Any())
                {
                    ModelState.AddModelError("Name", Localize("The menu name '{0}' is already in use.", model.Name));
                    return View(model);
                }

                if (id.DefaultWhenNull(0) == 0)
                {
                    model.Id = await configurationRepository.InsertMenuItem(model.ToDataModel());
                    await configurationManager.ReloadMenu();
                    ModelState.Clear();

                    return NotifyOfSuccess(Localize("The menu item has been created."), $"/Admin/MenuItem/{model.Id}");
                }
                else
                {
                    await configurationRepository.UpdateMenuItemById(model.ToDataModel());
                    await configurationManager.ReloadMenu();
                }

                model.SuccessMessage = Localize("The menu item has been saved.");
                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while saving menu item with ID '{Id}'.", id);
                throw;
            }
        }

        [Authorize]
        [HttpPost("DeleteMenuItem/{id:int}")]
        public async Task<ActionResult> DeleteRole(ConfirmActionViewModel model, int id)
        {
            try
            {
                try
                {
                    await SessionState.RequireAdminPermission();
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                if (model.UserSelection == true)
                {
                    await configurationRepository.DeleteMenuItemById(id);
                    await configurationManager.ReloadMenu();
                    return NotifyOfSuccess(Localize("The specified menu item has been deleted."), model.YesRedirectURL);
                }

                return Redirect($"{WikiConfiguration.BasePath}{model.NoRedirectURL}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while deleting menu item with ID '{Id}'.", id);
                throw;
            }
        }

        #endregion

        #region Config.

        [Authorize]
        [HttpGet("Config")]
        public async Task<ActionResult> Config()
        {
            try
            {
                try
                {
                    await SessionState.RequireAdminPermission();
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                var model = new ConfigurationViewModel()
                {
                    Themes = await configurationRepository.GetAllThemes(),
                    Roles = await usersRepository.GetAllRoles(),
                    TimeZones = TimeZoneItem.GetAll(),
                    Countries = CountryItem.GetAll(),
                    Languages = LanguageItem.GetAll(),
                    Nest = await configurationRepository.GetConfigurationNest()
                };
                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while retrieving configuration.");
                throw;
            }
        }

        [Authorize]
        [HttpPost("Config")]
        public async Task<ActionResult> Config(ConfigurationViewModel model)
        {
            try
            {
                try
                {
                    await SessionState.RequireAdminPermission();
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
                        Themes = await configurationRepository.GetAllThemes(),
                        Roles = await usersRepository.GetAllRoles(),
                        TimeZones = TimeZoneItem.GetAll(),
                        Countries = CountryItem.GetAll(),
                        Languages = LanguageItem.GetAll(),
                        Nest = await configurationRepository.GetConfigurationNest(),
                    };

                    var flatConfig = await configurationRepository.GetFlatConfiguration();

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
                            WikiConfiguration.SystemTheme = (await configurationRepository.GetAllThemes()).Single(o => o.Name == value);
                            if (string.IsNullOrEmpty(SessionState.Profile?.Theme))
                            {
                                SessionState.UserTheme = WikiConfiguration.SystemTheme;
                            }
                        }

                        if (fc.IsEncrypted)
                        {
                            value = SecurityUtility.EncryptString(SecurityUtility.MachineKey, value);
                        }

                        await configurationRepository.SaveConfigurationEntryValueByGroupAndEntry(fc.GroupName, fc.EntryName, value);
                        await configurationManager.ReloadAll();
                    }

                    MemCache.ClearCategory(MemCache.Category.Configuration);

                    model.SuccessMessage = Localize("The configuration has been saved successfully!");
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.Message);
                }

                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while saving configuration.");
                throw;
            }
        }

        #endregion

        #region Emojis.

        [Authorize]
        [HttpGet("Emojis")]
        public async Task<ActionResult> Emojis()
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(null, WikiPermission.Moderate);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                SessionState.Page.Name = Localize("Emojis");

                var pageNumber = GetQueryValue("page", 1);
                var orderBy = GetQueryValue<string>("OrderBy");
                var orderByDirection = GetQueryValue<string>("OrderByDirection");
                var searchString = GetQueryValue("SearchString", string.Empty);

                var model = new EmojisViewModel()
                {
                    Emojis = await emojiRepository.GetAllEmojisPaged(pageNumber, orderBy, orderByDirection, Utility.SplitToTokens(searchString)),
                    SearchString = searchString
                };

                model.PaginationPageCount = (model.Emojis.FirstOrDefault()?.PaginationPageCount ?? 0);

                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while retrieving emojis.");
                throw;
            }
        }

        [Authorize]
        [HttpGet("Emoji/{name}")]
        public async Task<ActionResult> Emoji(string name)
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(null, WikiPermission.Moderate);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                var emoji = await emojiRepository.GetEmojiByName(name);

                var model = new EmojiViewModel
                {
                    Emoji = emoji ?? new TwEmoji(),
                    Categories = string.Join(",", (await emojiRepository.GetEmojiCategoriesByName(name)).Select(o => o.Category).ToList()),
                    OriginalName = emoji?.Name ?? string.Empty
                };

                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while retrieving emoji with name '{Name}'.", name);
                throw;
            }
        }

        /// <summary>
        /// Update an existing emoji.
        /// </summary>
        [Authorize]
        [HttpPost("Emoji/{name}")]
        public async Task<ActionResult> Emoji(EmojiViewModel model)
        {
            try
            {
                try
                {
                    await SessionState.RequireAdminPermission();
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
                    var checkName = emojiRepository.GetEmojiByName(model.Emoji.Name.ToLowerInvariant());
                    if (checkName != null)
                    {
                        ModelState.AddModelError("Emoji.Name", Localize("Emoji name is already in use."));
                        return View(model);
                    }
                }

                var emoji = new TwUpsertEmoji
                {
                    Id = model.Emoji.Id,
                    Name = model.Emoji.Name.ToLowerInvariant(),
                    Categories = Utility.SplitToTokens($"{model.Categories} {model.Emoji.Name} {Text.SeparateCamelCase(model.Emoji.Name)}")
                };

                var file = Request.Form.Files["ImageData"];
                if (file != null && file.Length > 0)
                {
                    if (file.Length > WikiConfiguration.MaxEmojiFileSize)
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

                emoji.Id = await emojiRepository.UpsertEmoji(emoji);
                model.OriginalName = model.Emoji.Name;
                model.SuccessMessage = Localize("The emoji has been saved successfully!");
                model.Emoji.Id = (int)emoji.Id;
                ModelState.Clear();

                await configurationManager.ReloadEmojis();

                if (nameChanged)
                {
                    return NotifyOfSuccess(Localize("The emoji has been saved."), $"/Admin/Emoji/{TwNavigation.Clean(emoji.Name)}");
                }

                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while saving emoji with name '{Name}'.", model.OriginalName);
                throw;
            }
        }

        [Authorize]
        [HttpGet("AddEmoji")]
        public async Task<ActionResult> AddEmoji()
        {
            try
            {
                try
                {
                    await SessionState.RequireAdminPermission();
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
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while loading the Add Emoji page.");
                throw;
            }
        }

        /// <summary>
        /// Save user profile.
        /// </summary>
        [Authorize]
        [HttpPost("AddEmoji")]
        public async Task<ActionResult> AddEmoji(AddEmojiViewModel model)
        {
            try
            {
                try
                {
                    await SessionState.RequireAdminPermission();
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
                    var checkName = await emojiRepository.GetEmojiByName(model.Name.ToLowerInvariant());
                    if (checkName != null)
                    {
                        ModelState.AddModelError("Name", Localize("Emoji name is already in use."));
                        return View(model);
                    }
                }

                var emoji = new TwUpsertEmoji
                {
                    Id = model.Id,
                    Name = model.Name.ToLowerInvariant(),
                    Categories = Utility.SplitToTokens($"{model.Categories} {model.Name} {Text.SeparateCamelCase(model.Name)}")
                };

                var file = Request.Form.Files["ImageData"];
                if (file != null && file.Length > 0)
                {
                    if (file.Length > WikiConfiguration.MaxEmojiFileSize)
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

                await emojiRepository.UpsertEmoji(emoji);
                await configurationManager.ReloadEmojis();

                return NotifyOfSuccess(Localize("The emoji has been created."), $"/Admin/Emoji/{TwNavigation.Clean(emoji.Name)}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while saving new emoji with name '{Name}'.", model.Name);
                throw;
            }
        }

        [Authorize]
        [HttpPost("DeleteEmoji/{name}")]
        public async Task<ActionResult> DeleteRole(ConfirmActionViewModel model, string name)
        {
            try
            {
                try
                {
                    await SessionState.RequireAdminPermission();
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                var emoji = emojiRepository.GetEmojiByName(name);

                if (model.UserSelection == true && emoji != null)
                {
                    await emojiRepository.DeleteById(emoji.Id);
                    await configurationManager.ReloadEmojis();
                    return NotifyOfSuccess(Localize("The specified emoji has been deleted."), model.YesRedirectURL);
                }

                return Redirect($"{WikiConfiguration.BasePath}{model.NoRedirectURL}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while deleting emoji with name '{Name}'.", name);
                throw;
            }
        }

        #endregion

        #region EventLog.

        [Authorize]
        [HttpGet("EventLog")]
        public async Task<ActionResult> EventLog()
        {
            try
            {
                try
                {
                    await SessionState.RequireAdminPermission();
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                SessionState.Page.Name = Localize("Event Log");

                var pageNumber = GetQueryValue("page", 1);
                var orderBy = GetQueryValue<string>("OrderBy");
                var orderByDirection = GetQueryValue<string>("OrderByDirection");

                var model = new EventLogViewModel()
                {
                    LogEntries = await loggingRepository.GetLogEntriesPaged(pageNumber, orderBy, orderByDirection)
                };

                model.PaginationPageCount = (model.LogEntries.FirstOrDefault()?.PaginationPageCount ?? 0);

                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while retrieving event log entries.");
                throw;
            }
        }

        [Authorize]
        [HttpGet("EventLogEntry/{id}")]
        public async Task<ActionResult> EventLogEntry(int id)
        {
            try
            {
                try
                {
                    await SessionState.RequireAdminPermission();
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                SessionState.Page.Name = Localize("Event Log Entry");

                var model = new EventLogEntryViewModel()
                {
                    LogEntry = await loggingRepository.GetLogEntryById(id)
                };

                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while retrieving event log entry with ID '{Id}'.", id);
                throw;
            }
        }

        [Authorize]
        [HttpPost("PurgeEventLog")]
        public async Task<ActionResult> PurgeEventLog(ConfirmActionViewModel model)
        {
            try
            {
                try
                {
                    await SessionState.RequireAdminPermission();
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                if (model.UserSelection == true)
                {
                    await loggingRepository.PurgeLogs();
                    return NotifyOfSuccess(Localize("All event logs have been purged."), model.YesRedirectURL);
                }

                return Redirect($"{WikiConfiguration.BasePath}{model.NoRedirectURL}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred while purging event logs.");
                throw;
            }
        }

        #endregion

        #region LDAP.

        public record LdapTestRequest(string Username, string Password);

        // POST: /Admin/TestLdap
        [HttpPost("TestLdap")]
        public async Task<IActionResult> TestLdap([FromBody] LdapTestRequest req)
        {
            var ldapAuthenticationConfiguration = await
                configurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.LDAPAuthentication);

            try
            {

                if (WikiConfiguration.EnableLDAPAuthentication == false)
                {
                    return Json(new { ok = false, error = Localize("LDAP authentication is not enabled.") });
                }

                if (LDAPUtility.LdapCredentialChallenge(ldapAuthenticationConfiguration, Localizer,
                    req.Username, req.Password, out var samAccountName, out var objectGuid))
                {
                    //We successfully authenticated against LDAP.

                    if (objectGuid == null || objectGuid == Guid.Empty)
                    {
                        return Json(new { ok = false, error = Localize("LDAP challenge succeeded, but the user does not have an objectGUID attribute.") });
                    }

                    var loginInfo = new UserLoginInfo("LDAP", objectGuid.Value.ToString(), "Active Directory");

                    var foundUser = await UserManager.FindByLoginAsync(loginInfo.LoginProvider, loginInfo.ProviderKey);

                    if (foundUser == null)
                    {
                        //User does not exist in TightWiki.
                        return Json(new { ok = true, message = Localize("LDAP challenge succeeded (un-provisioned account)."), distinguishedName = samAccountName });
                    }
                    else
                    {
                        if (await usersRepository.GetBasicProfileByUserId(Guid.Parse(foundUser.Id)) != null)
                        {
                            //User and profile exist in TightWiki.
                            return Json(new { ok = true, message = Localize("LDAP challenge succeeded (fully provisioned account)."), distinguishedName = samAccountName });
                        }

                        //User exists in TightWiki, but the profile does not.
                        return Json(new { ok = true, message = Localize("LDAP challenge succeeded (partially provisioned account)."), distinguishedName = samAccountName });
                    }
                }
                return Json(new { ok = false, error = Localize("LDAP challenge failed.") });
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "An error occurred while testing LDAP credentials for user '{Username}'.", req.Username);
                return Json(new { ok = false, error = Localize("LDAP error: {0}.", ex.Message) });
            }
        }

        #endregion
    }
}
