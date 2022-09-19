using AsapWiki.Shared.ADO;
using AsapWiki.Shared.Models;
using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace AsapWiki.Shared.Repository
{
    public static partial class PageFileRepository
    {
        public static List<PageFile> GetPageFilesInfoByPageId(int pageId)
        {
            using (var handler = new SqlConnectionHandler())
            {
                return handler.Connection.Query<PageFile>("GetPageFilesInfoByPageId",
                    new { PageId = pageId }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
            }
        }

        public static PageFile GetPageFileByPageNavigationAndName(string pageNavigation, string imageName)
        {
            using (var handler = new SqlConnectionHandler())
            {
                return handler.Connection.Query<PageFile>("GetPageFileByPageNavigationAndName",
                    new
                    {
                        PageNavigation = pageNavigation,
                        ImageName = imageName
                    }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
            }
        }

        public static int UpsertPageFile(PageFile item)
        {
            using (var handler = new SqlConnectionHandler())
            {
                var param = new
                {
                    PageId = item.PageId,
                    Name = item.Name,
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
