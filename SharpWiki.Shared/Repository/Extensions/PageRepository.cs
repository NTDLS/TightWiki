using Dapper;
using SharpWiki.Shared.ADO;
using SharpWiki.Shared.Library;
using SharpWiki.Shared.Models;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace SharpWiki.Shared.Repository
{
    public static partial class PageRepository
    {
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

        public static int? GetPageIdFromNavigation(string navigation)
        {
            using (var handler = new SqlConnectionHandler())
            {
                var param = new
                {
                    Navigation = navigation
                };

                return handler.Connection.Query<int?>("GetPageIdFromNavigation",
                    param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
            }
        }

        public static Page GetPageRevisionByNavigation(string navigation, int? revision = null)
        {

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
