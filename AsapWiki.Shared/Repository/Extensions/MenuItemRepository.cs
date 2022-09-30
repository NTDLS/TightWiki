using AsapWiki.Shared.ADO;
using AsapWiki.Shared.Models;
using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace AsapWiki.Shared.Repository
{
    public static partial class MenuItemRepository
	{
		public static List<MenuItem> GetAllMenuItems()
		{
			using (var handler = new SqlConnectionHandler())
			{
				return handler.Connection.Query<MenuItem>("GetAllMenuItems",
					null, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
			}
		}
	}
}
