using NTDLS.SqliteDapperWrapper;
using TightWiki.Caching;
using TightWiki.Models;
using TightWiki.Models.DataModels;

namespace TightWiki.Repository
{
    public static class PageFileRepository
    {
        public static void DetachPageRevisionAttachment(string pageNavigation, string fileNavigation, int pageRevision)
        {
            var param = new
            {
                PageNavigation = pageNavigation,
                FileNavigation = fileNavigation,
                PageRevision = pageRevision
            };

            ManagedDataStorage.Pages.Execute("DetachPageRevisionAttachment.sql", param);
        }

        public static List<OrphanedPageAttachment> GetOrphanedPageAttachmentsPaged(
            int pageNumber, string? orderBy = null, string? orderByDirection = null)
        {
            var param = new
            {
                PageNumber = pageNumber,
                PageSize = GlobalConfiguration.PaginationSize
            };

            var query = RepositoryHelper.TransposeOrderby("GetOrphanedPageAttachments.sql", orderBy, orderByDirection);
            return ManagedDataStorage.Pages.Query<OrphanedPageAttachment>(query, param).ToList();
        }

        public static void PurgeOrphanedPageAttachments()
            => ManagedDataStorage.Pages.Execute("PurgeOrphanedPageAttachments.sql");

        public static void PurgeOrphanedPageAttachment(int pageFileId, int revision)
        {
            var param = new
            {
                PageFileId = pageFileId,
                Revision = revision
            };
            ManagedDataStorage.Pages.Execute("PurgeOrphanedPageAttachment.sql", param);
        }

        public static List<PageFileAttachmentInfo> GetPageFilesInfoByPageNavigationAndPageRevisionPaged(string pageNavigation, int pageNumber, int? pageSize = null, int? pageRevision = null)
        {
            pageSize ??= GlobalConfiguration.PaginationSize;

            var param = new
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                PageNavigation = pageNavigation,
                PageRevision = pageRevision
            };
            return ManagedDataStorage.Pages.Query<PageFileAttachmentInfo>("GetPageFilesInfoByPageNavigationAndPageRevisionPaged.sql", param).ToList();
        }

        public static PageFileAttachmentInfo? GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(string pageNavigation, string fileNavigation, int? pageRevision = null)
        {
            var param = new
            {
                PageNavigation = pageNavigation,
                FileNavigation = fileNavigation,
                PageRevision = pageRevision
            };

            return ManagedDataStorage.Pages.QuerySingleOrDefault<PageFileAttachmentInfo>("GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation.sql", param);
        }

        public static PageFileAttachment? GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(string pageNavigation, string fileNavigation, int? fileRevision = null)
        {
            var param = new
            {
                PageNavigation = pageNavigation,
                FileNavigation = fileNavigation,
                FileRevision = fileRevision
            };

            return ManagedDataStorage.Pages.QuerySingleOrDefault<PageFileAttachment>("GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation.sql", param);
        }

        public static PageFileAttachment? GetPageFileAttachmentByPageNavigationPageRevisionAndFileNavigation(string pageNavigation, string fileNavigation, int? pageRevision = null, bool allowCache = true)
        {
            if (allowCache)
            {
                var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Page, [pageNavigation, fileNavigation, pageRevision]);
                if (!WikiCache.TryGet<PageFileAttachment>(cacheKey, out var result))
                {
                    if ((result = GetPageFileAttachmentByPageNavigationPageRevisionAndFileNavigation(pageNavigation, fileNavigation, pageRevision, false)) != null)
                    {
                        WikiCache.Put(cacheKey, result);
                    }
                }

                return result;
            }

            var param = new
            {
                PageNavigation = pageNavigation,
                FileNavigation = fileNavigation,
                PageRevision = pageRevision
            };

            return ManagedDataStorage.Pages.QuerySingleOrDefault<PageFileAttachment>(
                "GetPageFileAttachmentByPageNavigationPageRevisionAndFileNavigation.sql", param);
        }

        public static List<PageFileAttachmentInfo> GetPageFileAttachmentRevisionsByPageAndFileNavigationPaged(string pageNavigation, string fileNavigation, int pageNumber)
        {
            var param = new
            {
                PageNavigation = pageNavigation,
                FileNavigation = fileNavigation,
                PageNumber = pageNumber,
                PageSize = GlobalConfiguration.PaginationSize
            };

            return ManagedDataStorage.Pages.Ephemeral(o =>
            {
                using var users_db = o.Attach("users.db", "users_db");

                var result = o.Query<PageFileAttachmentInfo>(
                    "GetPageFileAttachmentRevisionsByPageAndFileNavigationPaged.sql", param).ToList();

                return result;
            });
        }

        public static List<PageFileAttachmentInfo> GetPageFilesInfoByPageId(int pageId)
        {
            var param = new
            {
                PageId = pageId
            };

            return ManagedDataStorage.Pages.Query<PageFileAttachmentInfo>("GetPageFilesInfoByPageId.sql", param).ToList();
        }

        public static PageFileRevisionAttachmentInfo? GetPageFileInfoByFileNavigation(SqliteManagedInstance connection, int pageId, string fileNavigation)
        {
            var param = new
            {
                PageId = pageId,
                Navigation = fileNavigation,
            };

            return connection.QuerySingleOrDefault<PageFileRevisionAttachmentInfo>("GetPageFileInfoByFileNavigation.sql", param);
        }

        public static PageFileRevisionAttachmentInfo? GetPageCurrentRevisionAttachmentByFileNavigation(SqliteManagedInstance connection, int pageId, string fileNavigation)
        {
            var param = new
            {
                PageId = pageId,
                Navigation = fileNavigation,
            };

            return connection.QuerySingleOrDefault<PageFileRevisionAttachmentInfo>("GetPageCurrentRevisionAttachmentByFileNavigation.sql", param);
        }

        public static void UpsertPageFile(PageFileAttachment item, Guid userId)
        {
            bool hasFileChanged = false;

            ManagedDataStorage.Pages.Ephemeral(o =>
            {
                var transaction = o.BeginTransaction();

                try
                {
                    var pageFileInfo = GetPageFileInfoByFileNavigation(o, item.PageId, item.FileNavigation);
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
                        pageFileInfo = GetPageFileInfoByFileNavigation(o, item.PageId, item.FileNavigation)
                                        ?? throw new Exception("Failed find newly inserted page attachment.");

                        hasFileChanged = true;
                    }


                    int currentFileRevision = 0;
                    var newDataHash = Security.Helpers.Crc32(item.Data);

                    var currentlyAttachedFile = GetPageCurrentRevisionAttachmentByFileNavigation(o, item.PageId, item.FileNavigation);
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
                        int currentPageRevision = PageRepository.GetCurrentPageRevision(o, item.PageId);

                        var updatePageFileRevisionParam = new
                        {
                            PageFileId = pageFileInfo.PageFileId,
                            FileRevision = currentFileRevision
                        };
                        //The file has changed (or is newly inserted), bump the file revision.
                        o.Execute("UpdatePageFileRevision.sql", updatePageFileRevisionParam);

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
                        o.Execute("InsertPageFileRevision.sql", insertPageFileRevisionParam);

                        var associatePageFileAttachmentWithPageRevisionParam = new
                        {
                            PageId = item.PageId,
                            PageFileId = pageFileInfo.PageFileId,
                            PageRevision = currentPageRevision,
                            FileRevision = currentFileRevision,
                            PreviousFileRevision = currentlyAttachedFile?.Revision //This is so we can disassociate the previous file revision.
                        };

                        //Associate the latest version of the file with the latest version of the page.
                        o.Execute("AssociatePageFileAttachmentWithPageRevision.sql", associatePageFileAttachmentWithPageRevisionParam);
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
