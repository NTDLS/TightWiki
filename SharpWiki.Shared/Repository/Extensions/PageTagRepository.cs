using Dapper;
using SharpWiki.Shared.ADO;
using SharpWiki.Shared.Models;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace SharpWiki.Shared.Repository
{
    public static partial class PageTagRepository
	{
		public static List<TagAssociation> GetAssociatedTags(string tag)
		{
			using (var handler = new SqlConnectionHandler())
			{
				var param = new
				{
					Tag = tag
				};

				return handler.Connection.Query<TagAssociation>("GetAssociatedTags",
					param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
			}
		}

		public static List<Page> GetPageInfoByTag(string tag)
		{
			var tags = new List<string>();
			tags.Add(tag);
			return GetPageInfoByTags(tags);
		}

		public static List<Page> GetPageInfoByTags(List<string> tags)
		{
			using (var handler = new SqlConnectionHandler())
			{
				var param = new
				{
					Tags = string.Join(",", tags.Select(o => o.Trim()))
				};

				return handler.Connection.Query<Page>("GetPageInfoByTags",
					param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
			}
		}

		public static List<Page> GetPageInfoByTokens(List<string> tags)
		{
			using (var handler = new SqlConnectionHandler())
			{
				var param = new
				{
					Tokens = string.Join(",", tags.Select(o => o.Trim()))
				};

				return handler.Connection.Query<Page>("GetPageInfoByTokens",
					param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
			}
		}

		public static void UpdatePageTags(int pageId, List<string> tags)
		{
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
