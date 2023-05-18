using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TightWiki.Shared.ADO;
using TightWiki.Shared.Library;
using TightWiki.Shared.Models.Data;

namespace TightWiki.Shared.Repository
{
    public static partial class MenuItemRepository
    {
        public static List<MenuItem> GetAllMenuItems(bool allowCache = true)
        {
            if (allowCache)
            {
                string cacheKey = $"Config:GetAllMenuItems";
                var result = Cache.Get<List<MenuItem>>(cacheKey);
                if (result == null)
                {
                    result = GetAllMenuItems(false);
                    Cache.Put(cacheKey, result);
                }
                return result;
            }

            using var handler = new SqlConnectionHandler();
            return handler.Connection.Query<MenuItem>("GetAllMenuItems",
                null, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
        }
    }
}

