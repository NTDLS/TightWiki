using AsapWiki.Shared.ADO;
using AsapWiki.Shared.Models;
using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace AsapWiki.Shared.Repository
{
    public static partial class PageRepository
    {
        public static Page GetPageByNavigation(string navigation)
        {
            using (var handler = new SqlConnectionHandler())
            {
                var param = new
                {
                    Navigation = navigation
                };

                return handler.Connection.Query<Page>("GetPageByNavigation",
                    param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
            }
        }

        public static List<Page> GetTopRecentlyModifiedPages(int top)
        {
            using (var handler = new SqlConnectionHandler())
            {
                var param = new
                {
                    Top = top
                };

                return handler.Connection.Query<Page>("GetTopRecentlyModifiedPages",
                    param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList(); ;
            }
        }
    }
}
