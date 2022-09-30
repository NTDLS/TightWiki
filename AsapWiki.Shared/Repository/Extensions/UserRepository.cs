using AsapWiki.Shared.ADO;
using AsapWiki.Shared.Models;
using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;

namespace AsapWiki.Shared.Repository
{
    public static partial class UserRepository
	{
		public static List<Role> GetUserRolesByUserId(int userID)
		{
			string cacheKey = $"User:{userID}:{(new StackTrace()).GetFrame(0).GetMethod().Name}";

			var cacheItem = Singletons.GetCacheItem<List<Role>>(cacheKey);
			if (cacheItem != null)
			{
				return cacheItem;
			}

			using (var handler = new SqlConnectionHandler())
			{
				cacheItem = handler.Connection.Query<Role>("GetUserRolesByUserId",
					new { UserID = userID }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
				Singletons.PutCacheItem(cacheKey, cacheItem);

			}

			return cacheItem;
		}

		public static User GetUserById(int id)
		{
			string cacheKey = $"User:{id}:{(new StackTrace()).GetFrame(0).GetMethod().Name}";
			var cacheItem = Singletons.GetCacheItem<User>(cacheKey);
			if (cacheItem != null)
			{
				return cacheItem;
			}

			using (var handler = new SqlConnectionHandler())
			{
				cacheItem =  handler.Connection.Query<User>("GetUserById",
					new { Id = id }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
				Singletons.PutCacheItem(cacheKey, cacheItem);
			}

			return cacheItem;
		}

		public static User GetUserByAccountName(string accountName)
		{
			string cacheKey = $"User:{accountName}:{(new StackTrace()).GetFrame(0).GetMethod().Name}";
			var cacheItem = Singletons.GetCacheItem<User>(cacheKey);
			if (cacheItem != null)
			{
				return cacheItem;
			}

			using (var handler = new SqlConnectionHandler())
			{
				cacheItem = handler.Connection.Query<User>("GetUserByAccountName",
					new { AccountName = accountName }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
				Singletons.PutCacheItem(cacheKey, cacheItem);
			}

			return cacheItem;
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
