using AsapWiki.Shared.ADO;
using AsapWiki.Shared.Models;
using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;

namespace AsapWiki.Shared.Repository
{
    public static partial class PageRepository
    {
        public static void UpdatePageProcessingInstructions(int pageId, List<string> instructions)
        {
            string cacheKey = $"Page:{pageId}";
            Singletons.ClearCacheItems(cacheKey);

            using (var handler = new SqlConnectionHandler())
            {
                var param = new
                {
                    PageId = pageId,
                    Instructions = string.Join(",", instructions)
                };

                handler.Connection.Execute("UpdatePageProcessingInstructions",
                    param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }
        }

        public static List<ProcessingInstruction> GetPageProcessingInstructionsByPageId(int pageId)
        {
            string cacheKey = $"Page:{pageId}:{(new StackTrace()).GetFrame(0).GetMethod().Name}";

            var cacheItem = Singletons.GetCacheItem<List<ProcessingInstruction>>(cacheKey);
            if (cacheItem != null)
            {
                return cacheItem;
            }

            using (var handler = new SqlConnectionHandler())
            {
                var param = new
                {
                    PageId = pageId
                };

                cacheItem = handler.Connection.Query<ProcessingInstruction>("GetPageProcessingInstructionsByPageId",
                    param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();

                Singletons.PutCacheItem(cacheKey, cacheItem);

            }

            return cacheItem;
        }

        public static Page GetPageById(int pageId)
        {
            string cacheKey = $"Page:{pageId}:{(new StackTrace()).GetFrame(0).GetMethod().Name}";
            var cacheItem = Singletons.GetCacheItem<Page>(cacheKey);
            if (cacheItem != null)
            {
                return cacheItem;
            }

            using (var handler = new SqlConnectionHandler())
            {
                cacheItem = handler.Connection.Query<Page>("GetPageById",
                    new { PageId = pageId }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
                Singletons.PutCacheItem(cacheKey, cacheItem);
            }

            return cacheItem;
        }

        public static void SavePageTokens(List<PageToken> items)
        {
            var pageIds = items.Select(o => o.PageId).Distinct();

            foreach (var pageId in pageIds)
            {
                string cacheKey = $"Page:{pageId}";
                Singletons.ClearCacheItems(cacheKey);
            }

            using (var handler = new SqlConnectionHandler())
            {
                handler.Connection.Execute("SavePageTokens",
                    new { PageTokens = items.ToDataTable() }, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }
        }

        public static int SavePage(Page item)
        {
            string cacheKey = $"Page:{item.Id}";
            Singletons.ClearCacheItems(cacheKey);
            Singletons.ClearCacheItems("Page:Navigation:");

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
            Page cacheItem = null;

            int? pageId = GetPageIdFromNavigation(navigation);
            if (pageId != null)
            {
                string cacheKey = $"Page:{pageId}:{(new StackTrace()).GetFrame(0).GetMethod().Name}";

                using (var handler = new SqlConnectionHandler())
                {
                    var param = new
                    {
                        PageId = pageId
                    };

                    cacheItem = handler.Connection.Query<Page>("GetPageInfoById",
                        param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
                    Singletons.PutCacheItem(cacheKey, cacheItem);
                }
            }

            return cacheItem;
        }

        public static int? GetPageIdFromNavigation(string navigation)
        {
            string cacheKey = $"Page:Navigation:{navigation}:{(new StackTrace()).GetFrame(0).GetMethod().Name}";
            var cacheItem = Singletons.GetCacheItem<int?>(cacheKey);
            if (cacheItem != null)
            {
                return cacheItem;
            }

            using (var handler = new SqlConnectionHandler())
            {
                var param = new
                {
                    Navigation = navigation
                };

                cacheItem = handler.Connection.Query<int?>("GetPageIdFromNavigation",
                    param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
                Singletons.PutCacheItem(cacheKey, cacheItem);
            }

            return cacheItem;

        }

        public static Page GetPageByNavigation(string navigation)
        {
            Page cacheItem = null ;

            int? pageId = GetPageIdFromNavigation(navigation);
            if (pageId != null)
            {
                string cacheKey = $"Page:{pageId}:{(new StackTrace()).GetFrame(0).GetMethod().Name}";

                using (var handler = new SqlConnectionHandler())
                {
                    var param = new
                    {
                        PageId = pageId
                    };

                    cacheItem = handler.Connection.Query<Page>("GetPageById",
                        param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
                    Singletons.PutCacheItem(cacheKey, cacheItem);
                }
            }

            return cacheItem;
        }

        public static List<Page> GetTopRecentlyModifiedPages(int topCount)
        {
            string cacheKey = $"Page:{topCount}:{(new StackTrace()).GetFrame(0).GetMethod().Name}";
            var cacheItem = Singletons.GetCacheItem<List<Page>>(cacheKey);
            if (cacheItem != null)
            {
                return cacheItem;
            }

            using (var handler = new SqlConnectionHandler())
            {
                var param = new
                {
                    TopCount = topCount
                };

                cacheItem = handler.Connection.Query<Page>("GetTopRecentlyModifiedPages",
                    param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
                Singletons.PutCacheItem(cacheKey, cacheItem);
            }

            return cacheItem;
        }

        public static List<RelatedPage> GetRelatedPages(int pageId)
        {
            string cacheKey = $"Page:{pageId}:{(new StackTrace()).GetFrame(0).GetMethod().Name}";
            var cacheItem = Singletons.GetCacheItem<List<RelatedPage>>(cacheKey);
            if (cacheItem != null)
            {
                return cacheItem;
            }

            using (var handler = new SqlConnectionHandler())
            {
                var param = new
                {
                    PageId = pageId
                };

                cacheItem = handler.Connection.Query<RelatedPage>("GetRelatedPages",
                    param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
                Singletons.PutCacheItem(cacheKey, cacheItem);
            }

            return cacheItem;
        }
    }
}
