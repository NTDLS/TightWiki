using AsapWiki.Shared.ADO;
using AsapWiki.Shared.Models;
using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;

namespace AsapWiki.Shared.Repository
{
    public static partial class PageFileRepository
    {
        public static PageFile GetPageFileInfoByPageIdAndName(int pageId, string fileName)
        {
            string cacheKey = $"Page:{pageId}";
            Singletons.ClearCacheItems(cacheKey);

            using (var handler = new SqlConnectionHandler())
            {
                return handler.Connection.Query<PageFile>("GetPageFileInfoByPageIdAndName",
                    new
                    {
                        PageId = pageId,
                        FileName = fileName
                    }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
            }
        }

        public static List<PageFile> GetPageFilesInfoByPageId(int pageId)
        {
            string cacheKey = $"Page:{pageId}:{(new StackTrace()).GetFrame(0).GetMethod().Name}";
            var cacheItem = Singletons.GetCacheItem<List<PageFile>>(cacheKey);
            if (cacheItem != null)
            {
                return cacheItem;
            }

            using (var handler = new SqlConnectionHandler())
            {
                cacheItem = handler.Connection.Query<PageFile>("GetPageFilesInfoByPageId",
                    new { PageId = pageId }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
                Singletons.PutCacheItem(cacheKey, cacheItem);
            }

            return cacheItem;
        }

        public static void DeletePageFileByPageNavigationAndName(string pageNavigation, string fileName)
        {
            var page = PageRepository.GetPageInfoByNavigation(pageNavigation);
            if (page != null)
            {
                string cacheKey = $"Page:{page.Id}";
                Singletons.ClearCacheItems(cacheKey);
            }

            using (var handler = new SqlConnectionHandler())
            {
                handler.Connection.Query<PageFile>("DeletePageFileByPageNavigationAndName",
                    new
                    {
                        PageNavigation = pageNavigation,
                        FileName = fileName
                    }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure);
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
            string cacheKey = $"Page:{item.PageId}";
            Singletons.ClearCacheItems(cacheKey);

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
