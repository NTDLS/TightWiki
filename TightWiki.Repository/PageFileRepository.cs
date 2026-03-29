using NTDLS.SqliteDapperWrapper;
using TightWiki.Caching;
using TightWiki.Models.DataModels;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Repository
{
    public static class PageFileRepository
    {
        public static async Task DetachPageRevisionAttachment(string pageNavigation, string fileNavigation, int pageRevision)
        {
            var param = new
            {
                PageNavigation = pageNavigation,
                FileNavigation = fileNavigation,
                PageRevision = pageRevision
            };

            await ManagedDataStorage.Pages.ExecuteAsync("DetachPageRevisionAttachment.sql", param);
        }

        public static async Task<List<OrphanedPageAttachment>> GetOrphanedPageAttachmentsPaged(
            int pageNumber, string? orderBy = null, string? orderByDirection = null)
        {
            var paginationSize = await ConfigurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                PageNumber = pageNumber,
                PageSize = paginationSize
            };

            var query = RepositoryHelpers.TransposeOrderby("GetOrphanedPageAttachments.sql", orderBy, orderByDirection);
            return await ManagedDataStorage.Pages.QueryAsync<OrphanedPageAttachment>(query, param);
        }

        public static async Task PurgeOrphanedPageAttachments()
            => await ManagedDataStorage.Pages.ExecuteAsync("PurgeOrphanedPageAttachments.sql");

        public static async Task PurgeOrphanedPageAttachment(int pageFileId, int revision)
        {
            var param = new
            {
                PageFileId = pageFileId,
                Revision = revision
            };
            await ManagedDataStorage.Pages.ExecuteAsync("PurgeOrphanedPageAttachment.sql", param);
        }

        public static async Task<List<PageFileAttachmentInfo>> GetPageFilesInfoByPageNavigationAndPageRevisionPaged(string pageNavigation, int pageNumber, int? pageSize = null, int? pageRevision = null)
        {
            pageSize ??= await ConfigurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                PageNavigation = pageNavigation,
                PageRevision = pageRevision
            };
            return await ManagedDataStorage.Pages.QueryAsync<PageFileAttachmentInfo>("GetPageFilesInfoByPageNavigationAndPageRevisionPaged.sql", param);
        }

        public static async Task<PageFileAttachmentInfo?> GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(string pageNavigation, string fileNavigation, int? pageRevision = null)
        {
            var param = new
            {
                PageNavigation = pageNavigation,
                FileNavigation = fileNavigation,
                PageRevision = pageRevision
            };

            return await ManagedDataStorage.Pages.QuerySingleOrDefaultAsync<PageFileAttachmentInfo>("GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation.sql", param);
        }

        public static async Task<PageFileAttachment?> GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(string pageNavigation, string fileNavigation, int? fileRevision = null)
        {
            var param = new
            {
                PageNavigation = pageNavigation,
                FileNavigation = fileNavigation,
                FileRevision = fileRevision
            };

            return await ManagedDataStorage.Pages.QuerySingleOrDefaultAsync<PageFileAttachment>("GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation.sql", param);
        }

        public static async Task<PageFileAttachment?> GetPageFileAttachmentByPageNavigationPageRevisionAndFileNavigation(string pageNavigation, string fileNavigation, int? pageRevision = null)
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Page, [pageNavigation, fileNavigation, pageRevision]);

            return await WikiCache.AddOrGetAsync(cacheKey, async () =>
            {
                var param = new
                {
                    PageNavigation = pageNavigation,
                    FileNavigation = fileNavigation,
                    PageRevision = pageRevision
                };

                return await ManagedDataStorage.Pages.QuerySingleOrDefaultAsync<PageFileAttachment>(
                    "GetPageFileAttachmentByPageNavigationPageRevisionAndFileNavigation.sql", param);
            });
        }

        public static async Task<List<PageFileAttachmentInfo>> GetPageFileAttachmentRevisionsByPageAndFileNavigationPaged(string pageNavigation, string fileNavigation, int pageNumber)
        {
            var paginationSize = await ConfigurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                PageNavigation = pageNavigation,
                FileNavigation = fileNavigation,
                PageNumber = pageNumber,
                PageSize = paginationSize
            };

            return await ManagedDataStorage.Pages.EphemeralAsync(async o =>
            {
                using var users_db = o.Attach("users.db", "users_db");

                var result = await o.QueryAsync<PageFileAttachmentInfo>(
                    "GetPageFileAttachmentRevisionsByPageAndFileNavigationPaged.sql", param);

                return result;
            });
        }

        public static async Task<List<PageFileAttachmentInfo>> GetPageFilesInfoByPageId(int pageId)
        {
            var param = new
            {
                PageId = pageId
            };

            return await ManagedDataStorage.Pages.QueryAsync<PageFileAttachmentInfo>("GetPageFilesInfoByPageId.sql", param);
        }

        public static async Task<PageFileRevisionAttachmentInfo?> GetPageFileInfoByFileNavigation(SqliteManagedInstance connection, int pageId, string fileNavigation)
        {
            var param = new
            {
                PageId = pageId,
                Navigation = fileNavigation,
            };

            return await connection.QuerySingleOrDefaultAsync<PageFileRevisionAttachmentInfo>("GetPageFileInfoByFileNavigation.sql", param);
        }

        public static async Task<PageFileRevisionAttachmentInfo?> GetPageCurrentRevisionAttachmentByFileNavigation(SqliteManagedInstance connection, int pageId, string fileNavigation)
        {
            var param = new
            {
                PageId = pageId,
                Navigation = fileNavigation,
            };

            return await connection.QuerySingleOrDefaultAsync<PageFileRevisionAttachmentInfo>("GetPageCurrentRevisionAttachmentByFileNavigation.sql", param);
        }

        public static async Task UpsertPageFile(PageFileAttachment item, Guid userId)
        {
            bool hasFileChanged = false;

            await ManagedDataStorage.Pages.EphemeralAsync(async o =>
            {
                var transaction = o.BeginTransaction();

                try
                {
                    var pageFileInfo = await GetPageFileInfoByFileNavigation(o, item.PageId, item.FileNavigation);
                    if (pageFileInfo == null)
                    {
                        //If the page file does not exist, then insert it.

                        var InsertPageFileParam = new
                        {
                            PageId = item.PageId,
                            Name = item.Name,
                            FileNavigation = item.FileNavigation,
                            ContentType = item.ContentType,
                            Size = item.Size,
                            CreatedDate = item.CreatedDate,
                            Data = item.Data
                        };

                        o.Execute("InsertPageFile.sql", InsertPageFileParam);

                        //Get the id of the newly inserted page file.
                        pageFileInfo = await GetPageFileInfoByFileNavigation(o, item.PageId, item.FileNavigation)
                                        ?? throw new Exception("Failed find newly inserted page attachment.");

                        hasFileChanged = true;
                    }


                    int currentFileRevision = 0;
                    var newDataHash = Security.Helpers.Crc32(item.Data);

                    var currentlyAttachedFile = await GetPageCurrentRevisionAttachmentByFileNavigation(o, item.PageId, item.FileNavigation);
                    if (currentlyAttachedFile != null)
                    {
                        //The PageFile exists and a revision of it is attached to this page revision.
                        //Keep track of the file revision, and determine if the file has changed (via the file hash).

                        currentFileRevision = currentlyAttachedFile.Revision;
                        hasFileChanged = currentlyAttachedFile.DataHash != newDataHash;
                    }
                    else
                    {
                        //The file either does not exist or is not attached to the current page revision.
                        hasFileChanged = true;

                        //We determined earlier that the PageFile does exist, so keep track of the file revision.
                        currentFileRevision = pageFileInfo.Revision;
                    }

                    if (hasFileChanged)
                    {
                        currentFileRevision++;

                        //Get the current page revision so that we can associate the page file attachment with the current page revision.
                        int currentPageRevision = await PageRepository.GetCurrentPageRevision(o, item.PageId);

                        var updatePageFileRevisionParam = new
                        {
                            PageFileId = pageFileInfo.PageFileId,
                            FileRevision = currentFileRevision
                        };

                        //The file has changed (or is newly inserted), bump the file revision.
                        await o.ExecuteAsync("UpdatePageFileRevision.sql", updatePageFileRevisionParam);

                        var insertPageFileRevisionParam = new
                        {
                            PageFileId = pageFileInfo.PageFileId,
                            ContentType = item.ContentType,
                            Size = item.Size,
                            CreatedDate = item.CreatedDate,
                            CreatedByUserId = userId,
                            Data = item.Data,
                            FileRevision = currentFileRevision,
                            DataHash = newDataHash,
                        };

                        //Insert the actual file data.
                        await o.ExecuteAsync("InsertPageFileRevision.sql", insertPageFileRevisionParam);

                        var associatePageFileAttachmentWithPageRevisionParam = new
                        {
                            PageId = item.PageId,
                            PageFileId = pageFileInfo.PageFileId,
                            PageRevision = currentPageRevision,
                            FileRevision = currentFileRevision,
                            PreviousFileRevision = currentlyAttachedFile?.Revision //This is so we can disassociate the previous file revision.
                        };

                        //Associate the latest version of the file with the latest version of the page.
                        await o.ExecuteAsync("AssociatePageFileAttachmentWithPageRevision.sql", associatePageFileAttachmentWithPageRevisionParam);
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            });
        }
    }
}
