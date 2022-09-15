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
