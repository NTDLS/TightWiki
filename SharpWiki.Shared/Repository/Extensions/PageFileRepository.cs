using SharpWiki.Shared.ADO;
using SharpWiki.Shared.Models;
using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;

namespace SharpWiki.Shared.Repository
{
    public static partial class PageFileRepository
    {
        public static PageFileAttachment GetPageFileInfoByPageIdPageRevisionAndName(int pageId, string fileName, string pageRevision = null)
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

        public static List<PageFileAttachment> GetPageFilesInfoByPageIdAndPageRevision(int pageId, string pageRevision = null)
        {
            using (var handler = new SqlConnectionHandler())
            {
                return handler.Connection.Query<PageFileAttachment>("GetPageFilesInfoByPageIdAndPageRevision",
                    new { PageId = pageId, PageRevision = pageRevision }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
            }
        }

        public static void DeletePageFileByPageNavigationAndName(string pageNavigation, string fileName)
        {
            using (var handler = new SqlConnectionHandler())
            {
                handler.Connection.Execute("DeletePageFileByPageNavigationAndName",
                    new
                    {
                        PageNavigation = pageNavigation,
                        FileName = fileName
                    }, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }
        }

        public static PageFileAttachment GetPageFileAttachmentByPageNavigationPageRevisionAndName(string pageNavigation, string fileName, int? pageRevision = null)
        {
            using (var handler = new SqlConnectionHandler())
            {
                return handler.Connection.Query<PageFileAttachment>("GetPageFileAttachmentByPageNavigationPageRevisionAndName",
                    new
                    {
                        PageNavigation = pageNavigation,
                        FileName = fileName,
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
