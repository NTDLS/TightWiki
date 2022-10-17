using TightWiki.Shared.ADO;
using TightWiki.Shared.Models;
using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace TightWiki.Shared.Repository
{
	public static partial class UserRoleRepository
	{        
		public static List<UserRole> GetAllUserRole()
		{
            using (var handler = new SqlConnectionHandler())
            {
                return handler.Connection.Query<UserRole>("GetAllUserRole",
                    null, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
            }
		}

		public static UserRole GetUserRoleById(int id)
		{
            using (var handler = new SqlConnectionHandler())
            {
                return handler.Connection.Query<UserRole>("GetUserRoleById",
                    new { Id = id }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
            }
		}

		public static void UpdateUserRoleById(UserRole item)
		{
            using (var handler = new SqlConnectionHandler())
            {
				var param = new
				{
					Id = item.Id,
					UserId = item.UserId,
					RoleId = item.RoleId
				};

                handler.Connection.Execute("UpdateUserRoleById",
                    param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }
		}		

		public static int InsertUserRole(UserRole item)
		{
            using (var handler = new SqlConnectionHandler())
            {
				var param = new
				{
					UserId = item.UserId,
					RoleId = item.RoleId
				};

                return handler.Connection.ExecuteScalar<int>("InsertUserRole",
                    param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }
		}
		
		public static void DeleteUserRoleById(int id)
		{
            using (var handler = new SqlConnectionHandler())
            {
                handler.Connection.Execute("DeleteUserRoleById",
                    new { Id = id }, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }						
		}
	}
}

