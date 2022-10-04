using SharpWiki.Shared.ADO;
using SharpWiki.Shared.Models;
using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;

namespace SharpWiki.Shared.Repository
{
    public static partial class PageTagRepository
	{
		public static List<TagAssociation> GetAssociatedTags(string tag)
		{
			string cacheKey = $"Tag:{tag}:{(new StackTrace()).GetFrame(0).GetMethod().Name}";
			var cacheItem = Singletons.GetCacheItem<List<TagAssociation>>(cacheKey);
			if (cacheItem != null)
			{
				return cacheItem;
			}

			using (var handler = new SqlConnectionHandler())
			{
				var param = new
				{
					Tag = tag
				};

				cacheItem = handler.Connection.Query<TagAssociation>("GetAssociatedTags",
					param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
				Singletons.PutCacheItem(cacheKey, cacheItem);
			}

			return cacheItem;
		}

		public static List<Page> GetPageInfoByTag(string tag)
		{
			string cacheKey = $"Tag:{tag}:{(new StackTrace()).GetFrame(0).GetMethod().Name}";
			var cacheItem = Singletons.GetCacheItem<List<Page>>(cacheKey);
			if (cacheItem != null)
			{
				return cacheItem;
			}

			var tags = new List<string>();
			tags.Add(tag);
			cacheItem = GetPageInfoByTags(tags);
			Singletons.PutCacheItem(cacheKey, cacheItem);
			return cacheItem;
		}

		public static List<Page> GetPageInfoByTags(List<string> tags)
		{
			string cacheKey = $"Tag:{string.Join(",", tags)}:{(new StackTrace()).GetFrame(0).GetMethod().Name}";
			var cacheItem = Singletons.GetCacheItem<List<Page>>(cacheKey);
			if (cacheItem != null)
			{
				return cacheItem;
			}

			using (var handler = new SqlConnectionHandler())
			{
				var param = new
				{
					Tags = string.Join(",", tags.Select(o => o.Trim()))
				};

				cacheItem = handler.Connection.Query<Page>("GetPageInfoByTags",
					param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
				Singletons.PutCacheItem(cacheKey, cacheItem);
			}

			return cacheItem;
		}

		public static List<Page> GetPageInfoByTokens(List<string> tags)
		{
			string cacheKey = $"Tag:{string.Join(",", tags)}:{(new StackTrace()).GetFrame(0).GetMethod().Name}";
			var cacheItem = Singletons.GetCacheItem<List<Page>>(cacheKey);
			if (cacheItem != null)
			{
				return cacheItem;
			}

			using (var handler = new SqlConnectionHandler())
			{
				var param = new
				{
					Tokens = string.Join(",", tags.Select(o => o.Trim()))
				};

				cacheItem = handler.Connection.Query<Page>("GetPageInfoByTokens",
					param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
				Singletons.PutCacheItem(cacheKey, cacheItem);
			}

			return cacheItem;
		}

		public static void UpdatePageTags(int pageId, List<string> tags)
		{
			string cacheKey = $"Page:{pageId}";
			Singletons.ClearCacheItems(cacheKey);

			using (var handler = new SqlConnectionHandler())
			{
				var param = new
				{
					PageId = pageId,
					Tags = string.Join(",", tags.Select(o => o.Trim()))
				};

				handler.Connection.Execute("UpdatePageTags",
					param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
			}
		}
	}
}
