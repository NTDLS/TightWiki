using Dapper;
using SharpWiki.Shared.ADO;
using SharpWiki.Shared.Models;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;

namespace SharpWiki.Shared.Repository
{
    public static partial class MenuItemRepository
	{
		public static List<MenuItem> GetAllMenuItems()
		{
			string cacheKey = $"Menu:{(new StackTrace()).GetFrame(0).GetMethod().Name}";
			var cacheItem = Singletons.GetCacheItem<List<MenuItem>>(cacheKey);
			if (cacheItem != null)
			{
				return cacheItem;
			}

			using (var handler = new SqlConnectionHandler())
			{
				cacheItem = handler.Connection.Query<MenuItem>("GetAllMenuItems",
					null, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
				Singletons.PutCacheItem(cacheKey, cacheItem);
			}

			return cacheItem;
		}
	}
}
