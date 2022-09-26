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
        public static Page GetPageById(int id)
        {
            using (var handler = new SqlConnectionHandler())
            {
                return handler.Connection.Query<Page>("GetPageById",
                    new { Id = id }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
            }
        }

        public static void SavePageTokens(List<PageToken> items)
        {
            using (var handler = new SqlConnectionHandler())
            {
                handler.Connection.Execute("SavePageTokens",
                    new { PageTokens = items.ToDataTable() }, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }
        }

        public static int SavePage(Page item)
        {
            using (var handler = new SqlConnectionHandler())
            {
                var param = new
                {
                    Id = item.Id,
                    Name = item.Name,
                    Navigation = item.Navigation,
                    Description = item.Description,
                    Body = item.Body,
                    CreatedByUserId = item.CreatedByUserId,
                    CreatedDate = item.CreatedDate,
                    ModifiedByUserId = item.ModifiedByUserId,
                    ModifiedDate = item.ModifiedDate
                };

                return handler.Connection.ExecuteScalar<int>("SavePage",
                    param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }
        }

        /// <summary>
        /// Gets the page info without the content.
        /// </summary>
        /// <param name="navigation"></param>
        /// <returns></returns>
        public static Page GetPageInfoByNavigation(string navigation)
        {
            using (var handler = new SqlConnectionHandler())
            {
                var param = new
                {
                    Navigation = navigation
                };

                return handler.Connection.Query<Page>("GetPageInfoByNavigation",
                    param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
            }
        }

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

        public static List<Page> GetTopRecentlyModifiedPages(int topCount)
        {
            using (var handler = new SqlConnectionHandler())
            {
                var param = new
                {
                    TopCount = topCount
                };

                return handler.Connection.Query<Page>("GetTopRecentlyModifiedPages",
                    param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
            }
        }

        public static List<RelatedPage> GetRelatedPages(int pageId)
        {
            using (var handler = new SqlConnectionHandler())
            {
                var param = new
                {
                    PageId = pageId
                };

                return handler.Connection.Query<RelatedPage>("GetRelatedPages",
                    param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
            }
        }
    }
}
