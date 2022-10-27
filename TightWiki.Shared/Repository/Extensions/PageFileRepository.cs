using Dapper;
using TightWiki.Shared.ADO;
using TightWiki.Shared.Models.Data;
using TightWiki.Shared.Wiki;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace TightWiki.Shared.Repository
{
    public static partial class PageFileRepository
    {
        public static PageFileAttachment GetPageFileInfoByPageIdPageRevisionAndName(int pageId, string fileName, int? pageRevision = null)
        {
            using (var handler = new SqlConnectionHandler())
            {
                return handler.Connection.Query<PageFileAttachment>("GetPageFileInfoByPageIdPageRevisionAndName",
                    new
                    {
                        PageId = pageId,
                        FileName = fileName,
                        PageRevision = pageRevision
                    }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
            }
        }

        public static List<PageFileAttachment> GetPageFilesInfoByPageNavigationAndPageRevisionPaged(string pageNavigation, int pageNumber, int pageSize = 0, int? pageRevision = null)
        {
            using (var handler = new SqlConnectionHandler())
            {
                var param = new
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    PageNavigation = pageNavigation,
                    PageRevision = pageRevision
                };

                return handler.Connection.Query<PageFileAttachment>("GetPageFilesInfoByPageNavigationAndPageRevisionPaged",
                   param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
            }
        }

        public static List<PageFileAttachment> GetPageFilesInfoByPageIdAndPageRevision(int pageId, int? pageRevision = null)
        {
            using (var handler = new SqlConnectionHandler())
            {
                return handler.Connection.Query<PageFileAttachment>("GetPageFilesInfoByPageIdAndPageRevision",
                    new { PageId = pageId, PageRevision = pageRevision }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
            }
        }

        public static void DeletePageFileByPageNavigationAndFileName(string pageNavigation, string fileNavigation)
        {
            using (var handler = new SqlConnectionHandler())
            {
                handler.Connection.Execute("DeletePageFileByPageNavigationAndFileName",
                    new
                    {
                        PageNavigation = pageNavigation,
                        FileNavigation = fileNavigation
                    }, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }
        }

        public static PageFileAttachment GetPageFileAttachmentByPageNavigationPageRevisionAndFileNavigation(string pageNavigation, string fileNavigation, int? pageRevision = null)
        {
            using (var handler = new SqlConnectionHandler())
            {
                return handler.Connection.Query<PageFileAttachment>("GetPageFileAttachmentByPageNavigationPageRevisionAndFileNavigation",
                    new
                    {
                        PageNavigation = pageNavigation,
                        FileNavigation = fileNavigation,
                        PageRevision = pageRevision
                    }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
            }
        }

        public static int UpsertPageFile(PageFileAttachment item)
        {
            using (var handler = new SqlConnectionHandler())
            {
                var param = new
                {
                    PageId = item.PageId,
                    Name = item.Name,
                    FileNavigation = WikiUtility.CleanPartialURI(item.Name),
                    ContentType = item.ContentType,
                    Size = item.Size,
                    CreatedDate = item.CreatedDate,
                    Data = item.Data
                };

                return handler.Connection.ExecuteScalar<int>("UpsertPageFile",
                    param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }
        }
    }
}
