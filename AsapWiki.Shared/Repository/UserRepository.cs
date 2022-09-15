using AsapWiki.Shared.ADO;
using AsapWiki.Shared.Models;
using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace AsapWiki.Shared.Repository
{
	public static partial class UserRepository
	{        
		public static List<User> GetAllUser()
		{
            using (var handler = new SqlConnectionHandler())
            {
                return handler.Connection.Query<User>("GetAllUser",
                    null, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
            }
		}

		public static User GetUserById(int id)
		{
            using (var handler = new SqlConnectionHandler())
            {
                return handler.Connection.Query<User>("GetUserById",
                    new { Id = id }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
            }
		}

		public static void UpdateUserById(User item)
		{
            using (var handler = new SqlConnectionHandler())
            {
				var param = new
				{
					Id = item.Id,
					EmailAddress = item.EmailAddress,
					DisplayName = item.DisplayName,
					Password = item.Password
				};

                handler.Connection.Execute("UpdateUserById",
                    param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }
		}		

		public static int InsertUser(User item)
		{
            using (var handler = new SqlConnectionHandler())
            {
				var param = new
				{
					EmailAddress = item.EmailAddress,
					DisplayName = item.DisplayName,
					Password = item.Password
				};

                return handler.Connection.ExecuteScalar<int>("InsertUser",
                    param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }
		}
		
		public static void DeleteUserById(int id)
		{
            using (var handler = new SqlConnectionHandler())
            {
                handler.Connection.Execute("DeleteUserById",
                    new { Id = id }, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
            }						
		}
	}
}

