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
				cacheItem = handler.Connection.Query<User>("GetUserById",
					new { Id = id }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
				Singletons.PutCacheItem(cacheKey, cacheItem);
			}

			return cacheItem;
		}

		public static User GetUserByNavigation(string navigation)
		{
			string cacheKey = $"User:{navigation}:{(new StackTrace()).GetFrame(0).GetMethod().Name}";
			var cacheItem = Singletons.GetCacheItem<User>(cacheKey);
			if (cacheItem != null)
			{
				return cacheItem;
			}

			using (var handler = new SqlConnectionHandler())
			{
				cacheItem = handler.Connection.Query<User>("GetUserByNavigation",
					new { Navigation = navigation }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
				Singletons.PutCacheItem(cacheKey, cacheItem);
			}

			return cacheItem;
		}

		public static User GetUserByEmailAndPassword(string emailAddress, string password)
		{
			string passwordHash = Library.Security.Sha256(password);
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

		public static void UpdateUserAvatar(int userId, byte[] imageData)
		{
			Singletons.ClearCacheItems($"User:{userId}");

			using (var handler = new SqlConnectionHandler())
			{
				var param = new
				{
					UserId = userId,
					Avatar = imageData,
				};

				handler.Connection.Execute("UpdateUserAvatar",
					param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
			}
		}

		public static byte[] GetUserAvatarBynavigation(string navigation)
		{
			using (var handler = new SqlConnectionHandler())
			{
				return handler.Connection.Query<byte[]>("GetUserAvatarBynavigation",
					new
					{
						Navigation = navigation
					}, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
			}
		}

		public static void UpdateUser(User item)
		{
			using (var handler = new SqlConnectionHandler())
			{
				var param = new
				{
					Id = item.Id,
					EmailAddress = item.EmailAddress,
					AccountName = item.AccountName,
					Navigation = item.Navigation,
					PasswordHash = item.PasswordHash,
					FirstName = item.FirstName,
					LastName = item.LastName,
					TimeZone = item.TimeZone,
					Country = item.Country,
					AboutMe = item.AboutMe,
					ModifiedDate = item.ModifiedDate
				};

				handler.Connection.Execute("UpdateUser",
					param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
			}
		}
	}
}