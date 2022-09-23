using AsapWiki.Shared.ADO;
using AsapWiki.Shared.Models;
using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace AsapWiki.Shared.Repository
{
    public static partial class RoleRepository
	{

		public static List<Role> GetUserRolesByUserId(int userID)
		{
			using (var handler = new SqlConnectionHandler())
			{
				return handler.Connection.Query<Role>("GetUserRolesByUserId",
					new { UserID = userID }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
			}
		}
	}
}

