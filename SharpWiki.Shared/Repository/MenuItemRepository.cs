using SharpWiki.Shared.ADO;
using SharpWiki.Shared.Models;
using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace SharpWiki.Shared.Repository
{
	public static partial class MenuItemRepository
	{        
		public static List<MenuItem> GetAllMenuItem()
		{
            using (var handler = new SqlConnectionHandler())
            {
                return handler.Connection.Query<MenuItem>("GetAllMenuItem",
                    null, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
            }
		}

		public static MenuItem GetMenuItemById(int id)
		{
            using (var handler = new SqlConnectionHandler())
            {
                return handler.Connection.Query<MenuItem>("GetMenuItemById",
                    new { Id = id }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
            }
		}

		public static void UpdateMenuItemById(MenuItem item)
		{
            using (var handler = new SqlConnectionHandler())
            {
				var param = new
				{
					Id = item.Id,
					Name = item.Name,
					Link = item.Link,
					Ordinal = item.Ordinal
				};

                handler.Connection.Execute("UpdateMenuItemById",
                    param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }
		}		

		public static int InsertMenuItem(MenuItem item)
		{
            using (var handler = new SqlConnectionHandler())
            {
				var param = new
				{
					Name = item.Name,
					Link = item.Link,
					Ordinal = item.Ordinal
				};

                return handler.Connection.ExecuteScalar<int>("InsertMenuItem",
                    param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }
		}
		
		public static void DeleteMenuItemById(int id)
		{
            using (var handler = new SqlConnectionHandler())
            {
                handler.Connection.Execute("DeleteMenuItemById",
                    new { Id = id }, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }						
		}
	}
}

