using NTDLS.SqliteDapperWrapper;
using TightWiki.Library;
using TightWiki.Models.DataModels;

namespace TightWiki.Repository
{
    public static class PageFileRepository
    {
        public static void DeletePageFileByPageNavigationAndFileName(string pageNavigation, string fileNavigation)
        {
            var param = new
            {
                PageNavigation = pageNavigation,
                FileNavigation = fileNavigation
            };

            ManagedDataStorage.Pages.Execute("DeletePageFileByPageNavigationAndFileName.sql", param);
        }

        public static List<PageFileAttachmentInfo> GetPageFilesInfoByPageNavigationAndPageRevisionPaged(string pageNavigation, int pageNumber, int? pageSize = null, int? pageRevision = null)
        {
            pageSize ??= ConfigurationRepository.Get<int>("Customization", "Pagination Size");

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

        public static List<PageFileAttachmentInfo> GetPageFileAttachmentRevisionsByPageAndFileNavigationPaged(string pageNavigation, string fileNavigation, int pageNumber, int? pageSize = null)
        {
            pageSize ??= ConfigurationRepository.Get<int>("Customization", "Pagination Size");

            var param = new
            {
                PageNavigation = pageNavigation,
                FileNavigation = fileNavigation,
                PageNumber = pageNumber,
                PageSize = pageSize
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

        public static PageFileRevisionAttachmentInfo? GetPageFileRevisionInfoByFileNavigation(int pageId, string fileNavigation)
        {
            var param = new
            {
                PageId = pageId,
                Navigation = fileNavigation,
            };

            return ManagedDataStorage.Pages.QuerySingleOrDefault<PageFileRevisionAttachmentInfo>("GetPageFileRevisionInfoByFileNavigation.sql", param);
        }

        public static PageFileAttachmentLimitedInfo? GetPageFileInfoByFileNavigation(ManagedDataStorageInstance connection, int pageId, string fileNavigation)
        {
            var param = new
            {
                PageId = pageId,
                Navigation = fileNavigation,
            };

            return connection.QuerySingleOrDefault<PageFileAttachmentLimitedInfo>("GetPageFileInfoByFileNavigation.sql", param);
        }

        public static void UpsertPageFile(PageFileAttachment item, Guid userId)
        {
            bool hasFileChanged = false;

            ManagedDataStorage.Pages.Ephemeral(o =>
            {
                var pageFileInfo = GetPageFileInfoByFileNavigation(o, item.PageId, item.FileNavigation);
                var transaction = o.BeginTransaction();
                try
                {
                    int currentFileRevision = 0;
                    var newDataHash = Utility.SimpleChecksum(item.Data);

                    if (pageFileInfo == null)
                    {
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

                        //File does NOT exist, insert it.
                        o.Execute("InsertPageFile.sql", InsertPageFileParam);

                        //Get the id of the newly inserted page file.
                        pageFileInfo = GetPageFileInfoByFileNavigation(o, item.PageId, item.FileNavigation)
                                        ?? throw new Exception("Failed find newly inserted page attachment.");

                        hasFileChanged = true;
                    }
                    else
                    {
                        var pageFileRevisionInfo = GetPageFileRevisionInfoByFileNavigation(item.PageId, item.FileNavigation)
                                                    ?? throw new Exception("Failed find newly updated page attachment.");

                        currentFileRevision = pageFileRevisionInfo.Revision;

                        hasFileChanged = pageFileRevisionInfo.DataHash != newDataHash;
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
