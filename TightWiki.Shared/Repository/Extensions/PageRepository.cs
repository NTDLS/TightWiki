using Dapper;
using TightWiki.Shared.ADO;
using TightWiki.Shared.Library;
using TightWiki.Shared.Models.Data;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace TightWiki.Shared.Repository
{
    public static partial class PageRepository
    {
        public static List<Page> PageSearch(List<PageToken> items)
        {
            using (var handler = new SqlConnectionHandler())
            {
                var param = new
                {
                    SearchTerms = items.ToDataTable()
                };

                return handler.Connection.Query<Page>("PageSearchPaged",
                    param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
            }
        }

        public static List<Page> GetAllPagesByInstructionPaged(int pageNumber, int pageSize = 0, string instruction = null)
        {
            using (var handler = new SqlConnectionHandler())
            {
                var param = new
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    Instruction = instruction
                };

                return handler.Connection.Query<Page>("GetAllPagesByInstructionPaged",
                    param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
            }
        }

        public static List<Page> PageSearchPaged(List<PageToken> items, int pageNumber, int pageSize = 0)
        {
            using (var handler = new SqlConnectionHandler())
            {
                var param = new
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    SearchTerms = items.ToDataTable()
                };

                return handler.Connection.Query<Page>("PageSearchPaged",
                    param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
            }
        }

        /// <summary>
        /// Unlike the search, this method retunrs all pages and allows them to be paired down using the search terms.
        /// Whereas the search requires a search term to get results. The matching here is also exact, no score based matching.
        /// </summary>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        /// <param name="searchTerms"></param>
        /// <returns></returns>
        public static List<Page> GetAllPagesPaged(int pageNumber, int pageSize = 0, string searchTerms = null)
        {
            using (var handler = new SqlConnectionHandler())
            {
                var param = new
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    SearchTerms = searchTerms
                };

                return handler.Connection.Query<Page>("GetAllPagesPaged",
                    param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
            }
        }

        public static List<Page> GetAllPages()
        {
            using (var handler = new SqlConnectionHandler())
            {

                return handler.Connection.Query<Page>("GetAllPages",
                    null, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
            }
        }

        public static void UpdatePageProcessingInstructions(int pageId, List<string> instructions)
        {
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
            using (var handler = new SqlConnectionHandler())
            {
                var param = new
                {
                    PageId = pageId
                };

                return handler.Connection.Query<ProcessingInstruction>("GetPageProcessingInstructionsByPageId",
                    param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
            }
        }

        public static Page GetPageRevisionInfoById(int pageId, int? revision = null)
        {
            using (var handler = new SqlConnectionHandler())
            {
                return handler.Connection.Query<Page>("GetPageRevisionInfoById",
                    new { PageId = pageId, Revision = revision }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
            }
        }

        public static List<PageTag> GetPageTagsById(int pageId)
        {
            using (var handler = new SqlConnectionHandler())
            {
                return handler.Connection.Query<PageTag>("GetPageTagsById",
                    new { PageId = pageId }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
            }
        }

        public static Page GetPageInfoById(int pageId)
        {
            using (var handler = new SqlConnectionHandler())
            {
                return handler.Connection.Query<Page>("GetPageInfoById",
                    new { PageId = pageId }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
            }
        }

        public static Page GetPageRevisionById(int pageId, int? revision = null)
        {
            using (var handler = new SqlConnectionHandler())
            {
                return handler.Connection.Query<Page>("GetPageRevisionById",
                    new { PageId = pageId, Revision = revision }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
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

        public static void TruncateAllPageHistory(string confirm)
        {
            using (var handler = new SqlConnectionHandler())
            {
                handler.Connection.Execute("TruncateAllPageHistory",
                    new { Confirm = confirm }, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
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

        public static List<PageRevisionHistory> GetPageRevisionHistoryInfoByNavigationPaged(string navigation, int pageNumber, int pageSize = 0)
        {
            using var handler = new SqlConnectionHandler();

            var param = new
            {
                Navigation = navigation,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return handler.Connection.Query<PageRevisionHistory>("GetPageRevisionHistoryInfoByNavigationPaged",
                param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
        }

        /// <summary>
        /// Gets the page info without the content.
        /// </summary>
        /// <param name="navigation"></param>
        /// <returns></returns>
        public static Page GetPageInfoByNavigation(string navigation)
        {
            using var handler = new SqlConnectionHandler();

            var param = new
            {
                Navigation = navigation
            };

            return handler.Connection.Query<Page>("GetPageInfoByNavigation",
                param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
        }

        public static void DeletePageById(int pageId)
        {
            using (var handler = new SqlConnectionHandler())
            {
                var param = new
                {
                    PageId = pageId
                };

                handler.Connection.Execute("DeletePageById",
                    param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }
        }

        public static int GetCountOfPageAttachmentsById(int pageId)
        {
            using (var handler = new SqlConnectionHandler())
            {
                var param = new
                {
                    PageId = pageId
                };

                return handler.Connection.Query<int?>("GetCountOfPageAttachmentsById",
                    param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault() ?? 0;
            }
        }

        public static Page GetPageRevisionByNavigation(string navigation, int? revision = null, bool allowCache = true)
        {
            if (allowCache)
            {
                string cacheKey = $"Page:{navigation}:{revision}:GetPageRevisionByNavigation";
                var result = Cache.Get<Page> (cacheKey);
                if (result == null)
                {
                    result = GetPageRevisionByNavigation(navigation, revision, false);
                    Cache.Put(cacheKey, result);
                }

                return result;
            }

            using (var handler = new SqlConnectionHandler())
            {
                var param = new
                {
                    Navigation = navigation,
                    Revision = revision
                };

                return handler.Connection.Query<Page>("GetPageRevisionByNavigation",
                    param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
            }
        }

        public static List<Page> GetTopRecentlyModifiedPagesInfo(int topCount)
        {
            using (var handler = new SqlConnectionHandler())
            {
                var param = new
                {
                    TopCount = topCount
                };

                return handler.Connection.Query<Page>("GetTopRecentlyModifiedPagesInfo",
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
