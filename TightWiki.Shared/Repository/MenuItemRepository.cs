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

        public static MenuItem GetMenuItemById(int id)
        {
            using var handler = new SqlConnectionHandler();
            return handler.Connection.Query<MenuItem>("GetMenuItemById",
                new { id = id }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
        }

        public static void DeleteMenuItemById(int id)
        {
            using var handler = new SqlConnectionHandler();

            handler.Connection.Query<MenuItem>("DeleteMenuItemById",
                new { Id = id }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();

            Cache.ClearClass("Config:");
            GlobalSettings.MenuItems = GetAllMenuItems();
        }

        public static void UpdateMenuItemById(MenuItem menuItem)
        {
            using var handler = new SqlConnectionHandler();
            handler.Connection.Query<MenuItem>("UpdateMenuItemById",
                menuItem, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();

            Cache.ClearClass("Config:");
            GlobalSettings.MenuItems = GetAllMenuItems();
        }

        public static int InsertMenuItem(MenuItem menuItem)
        {
            var param = new
            {
                menuItem.Name,
                menuItem.Link,
                menuItem.Ordinal
            };

            using var handler = new SqlConnectionHandler();
            var result = handler.Connection.ExecuteScalar<int>("InsertMenuItem", param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);

            Cache.ClearClass("Config:");
            GlobalSettings.MenuItems = GetAllMenuItems();
            return result;
        }
    }
}

