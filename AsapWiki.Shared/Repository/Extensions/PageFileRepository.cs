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
    }
}
