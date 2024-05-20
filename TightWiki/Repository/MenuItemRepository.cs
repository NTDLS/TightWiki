using TightWiki.DataModels;
using TightWiki.DataStorage;
using TightWiki.Library;

namespace TightWiki.Repository
{
    public static class MenuItemRepository
    {
        public static List<MenuItem> GetAllMenuItems()
        {
            return ManagedDataStorage.Default.Query<MenuItem>("GetAllMenuItems").ToList();
        }

        public static MenuItem GetMenuItemById(int id)
        {
            var param = new
            {
                Id = id
            };

            return ManagedDataStorage.Default.QuerySingle<MenuItem>("GetMenuItemById", param);
        }

        public static void DeleteMenuItemById(int id)
        {
            var param = new
            {
                Id = id
            };

            ManagedDataStorage.Default.Execute("DeleteMenuItemById", param);

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

            var menuItemId = ManagedDataStorage.Default.ExecuteScalar<int>("UpdateMenuItemById", param);

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

            var menuItemId = ManagedDataStorage.Default.ExecuteScalar<int>("InsertMenuItem", param);

            WikiCache.ClearCategory(WikiCache.Category.Configuration);
            GlobalSettings.MenuItems = GetAllMenuItems();

            return menuItemId;
        }
    }
}
