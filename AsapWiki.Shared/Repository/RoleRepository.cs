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
		public static List<Role> GetAllRole()
		{
            using (var handler = new SqlConnectionHandler())
            {
                return handler.Connection.Query<Role>("GetAllRole",
                    null, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
            }
		}

		public static Role GetRoleById(int id)
		{
            using (var handler = new SqlConnectionHandler())
            {
                return handler.Connection.Query<Role>("GetRoleById",
                    new { Id = id }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
            }
		}

		public static void UpdateRoleById(Role item)
		{
            using (var handler = new SqlConnectionHandler())
            {
				var param = new
				{
					Id = item.Id,
					Name = item.Name,
					Description = item.Description
				};

                handler.Connection.Execute("UpdateRoleById",
                    param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }
		}		

		public static int InsertRole(Role item)
		{
            using (var handler = new SqlConnectionHandler())
            {
				var param = new
				{
					Name = item.Name,
					Description = item.Description
				};

                return handler.Connection.ExecuteScalar<int>("InsertRole",
                    param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }
		}
		
		public static void DeleteRoleById(int id)
		{
            using (var handler = new SqlConnectionHandler())
            {
                handler.Connection.Execute("DeleteRoleById",
                    new { Id = id }, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }						
		}
	}
}

