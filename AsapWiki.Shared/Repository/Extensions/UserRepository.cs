using AsapWiki.Shared.ADO;
using AsapWiki.Shared.Models;
using Dapper;
using System.Data;
using System.Linq;

namespace AsapWiki.Shared.Repository
{
    public static partial class UserRepository
	{
		public static User GetUserById(int id)
		{
			using (var handler = new SqlConnectionHandler())
			{
				return handler.Connection.Query<User>("GetUserById",
					new { Id = id }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
			}
		}

		public static User GetUserByEmailAndPassword(string emailAddress, string password)
		{
			string passwordHash = Classes.Security.Sha256(password);

			using (var handler = new SqlConnectionHandler())
			{
				var param = new
				{
					EmailAddress = emailAddress,
					PasswordHash = passwordHash
				};

				return handler.Connection.Query<User>("GetUserByEmailAndPasswordHash",
					param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
			}
		}
	}
}
