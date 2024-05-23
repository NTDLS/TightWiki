using TightWiki.DataStorage;
using TightWiki.Library;
using TightWiki.Models.DataModels;

namespace TightWiki.Repository
{
    public static class MenuItemRepository
    {
        public static List<MenuItem> GetAllMenuItems()
        {
            return ManagedDataStorage.Config.Query<MenuItem>("GetAllMenuItems").ToList();
        }

        public static MenuItem GetMenuItemById(int id)
        {
            var param = new
            {
                Id = id
            };

            return ManagedDataStorage.Config.QuerySingle<MenuItem>("GetMenuItemById", param);
        }

        public static void DeleteMenuItemById(int id)
        {
            var param = new
            {
                Id = id
            };

            ManagedDataStorage.Config.Execute("DeleteMenuItemById", param);

            WikiCache.ClearCategory(WikiCache.Category.Configuration);
            GlobalSettings.MenuItems = GetAllMenuItems();
        }

        public static int UpdateMenuItemById(MenuItem menuItem)
        {
            var param = new
            {
                menuItem.Id,
                menuItem.Name,
                menuItem.Link,
                menuItem.Ordinal
            };

            var menuItemId = ManagedDataStorage.Config.ExecuteScalar<int>("UpdateMenuItemById", param);

            WikiCache.ClearCategory(WikiCache.Category.Configuration);
            GlobalSettings.MenuItems = GetAllMenuItems();

            return menuItemId;
        }

        public static int InsertMenuItem(MenuItem menuItem)
        {
            var param = new
            {
                menuItem.Name,
                menuItem.Link,
                menuItem.Ordinal
            };

            var menuItemId = ManagedDataStorage.Config.ExecuteScalar<int>("InsertMenuItem", param);

            WikiCache.ClearCategory(WikiCache.Category.Configuration);
            GlobalSettings.MenuItems = GetAllMenuItems();

            return menuItemId;
        }
    }
}
