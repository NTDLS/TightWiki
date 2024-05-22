using TightWiki.DataStorage;
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

            ManagedDataStorage.Default.Execute("DeletePageFileByPageNavigationAndFileName", param);
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
            return ManagedDataStorage.Default.Query<PageFileAttachmentInfo>("GetPageFilesInfoByPageNavigationAndPageRevisionPaged", param).ToList();
        }

        public static PageFileAttachmentInfo? GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(string pageNavigation, string fileNavigation, int? pageRevision = null)
        {
            var param = new
            {
                PageNavigation = pageNavigation,
                FileNavigation = fileNavigation,
                PageRevision = pageRevision
            };

            return ManagedDataStorage.Default.QuerySingleOrDefault<PageFileAttachmentInfo>("GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation", param);
        }

        public static PageFileAttachment? GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(string pageNavigation, string fileNavigation, int? fileRevision = null)
        {
            var param = new
            {
                PageNavigation = pageNavigation,
                FileNavigation = fileNavigation,
                FileRevision = fileRevision
            };

            return ManagedDataStorage.Default.QuerySingleOrDefault<PageFileAttachment>("GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation", param);
        }

        public static PageFileAttachment? GetPageFileAttachmentByPageNavigationPageRevisionAndFileNavigation(string pageNavigation, string fileNavigation, int? pageRevision = null)
        {
            var param = new
            {
                PageNavigation = pageNavigation,
                FileNavigation = fileNavigation,
                PageRevision = pageRevision
            };

            return ManagedDataStorage.Default.QuerySingleOrDefault<PageFileAttachment>(
                "GetPageFileAttachmentByPageNavigationPageRevisionAndFileNavigation", param);
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

            return ManagedDataStorage.Default.Query<PageFileAttachmentInfo>(
                "GetPageFileAttachmentRevisionsByPageAndFileNavigationPaged", param).ToList();
        }

        public static List<PageFileAttachmentInfo> GetPageFilesInfoByPageId(int pageId)
        {
            var param = new
            {
                PageId = pageId
            };

            return ManagedDataStorage.Default.Query<PageFileAttachmentInfo>("GetPageFilesInfoByPageId", param).ToList();
        }

        public static PageFileRevisionAttachmentInfo? GetPageFileRevisionInfoByFileNavigation(int pageId, string fileNavigation)
        {
            var param = new
            {
                PageId = pageId,
                Navigation = fileNavigation,
            };

            return ManagedDataStorage.Default.QuerySingleOrDefault<PageFileRevisionAttachmentInfo>("GetPageFileRevisionInfoByFileNavigation", param);
        }


        public static PageFileAttachmentLimitedInfo? GetPageFileInfoByFileNavigation(ManagedDataStorageInstance connection, int pageId, string fileNavigation)
        {
            var param = new
            {
                PageId = pageId,
                Navigation = fileNavigation,
            };

            return connection.QuerySingleOrDefault<PageFileAttachmentLimitedInfo>("GetPageFileInfoByFileNavigation", param);
        }

        public static void UpsertPageFile(PageFileAttachment item, Guid userId)
        {
            bool hasFileChanged = false;

            ManagedDataStorage.Default.Ephemeral(o =>
            {
                var pageFileInfo = GetPageFileInfoByFileNavigation(o, item.PageId, item.FileNavigation);
                var transaction = o.BeginTransaction();
                try
                {
                    int currentFileRevision = 1;
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
                        o.Execute("InsertPageFile", InsertPageFileParam);

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
                        o.Execute("UpdatePageFileRevision", updatePageFileRevisionParam);

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
                        o.Execute("InsertPageFileRevision", insertPageFileRevisionParam);

                        var associatePageFileAttachmentWithPageRevisionparam = new
                        {
                            PageId = item.PageId,
                            PageFileId = pageFileInfo.PageFileId,
                            PageRevision = currentPageRevision,
                            FileRevision = currentFileRevision,
                        };
                        //Associate the latest version of the file with the latest version of the page.
                        o.Execute("AssociatePageFileAttachmentWithPageRevision", associatePageFileAttachmentWithPageRevisionparam);
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
