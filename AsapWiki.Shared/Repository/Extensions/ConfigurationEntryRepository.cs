using AsapWiki.Shared.ADO;
using AsapWiki.Shared.Library;
using AsapWiki.Shared.Models;
using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;

namespace AsapWiki.Shared.Repository
{
    public static partial class ConfigurationEntryRepository
	{
		public static List<ConfigurationEntry> GetConfigurationEntryValuesByGroupName(string groupName)
		{
			string cacheKey = $"Configuration:{groupName}:{(new StackTrace()).GetFrame(0).GetMethod().Name}";
			var cacheItem = Singletons.GetCacheItem<List<ConfigurationEntry>>(cacheKey);
			if (cacheItem != null)
			{
				return cacheItem;
			}

			using (var handler = new SqlConnectionHandler())
			{
				var param = new
				{
					GroupName = groupName
				};

				cacheItem = handler.Connection.Query<ConfigurationEntry>("GetConfigurationEntryValuesByGroupName",
					param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
				Singletons.PutCacheItem(cacheKey, cacheItem);
			}

			return cacheItem;
		}

		public static string GetConfigurationEntryValuesByGroupNameAndEntryName(string groupName, string entryName)
		{
			string cacheKey = $"Configuration:{groupName}:{entryName}:{(new StackTrace()).GetFrame(0).GetMethod().Name}";
			var cacheItem = Singletons.GetCacheItem<string>(cacheKey);
			if (cacheItem != null)
			{
				return cacheItem;
			}

			using (var handler = new SqlConnectionHandler())
			{
				var param = new
				{
					GroupName = groupName,
					EntryName = entryName
				};

				cacheItem = handler.Connection.Query<ConfigurationEntry>("GetConfigurationEntryValuesByGroupNameAndEntryName",
					param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault()?.Value;
				Singletons.PutCacheItem(cacheKey, cacheItem);
			}

			return cacheItem;
		}

		public static T Get<T>(string groupName, string entryName)
		{
			string cacheKey = $"Configuration:{groupName}:{entryName}:{(new StackTrace()).GetFrame(0).GetMethod().Name}";
			T cacheItem;
			if (Singletons.Cache.Contains(cacheKey))
			{
				cacheItem = Singletons.GetCacheItem<T>(cacheKey);
				if (cacheItem != null)
				{
					return cacheItem;
				}
			}

			using (var handler = new SqlConnectionHandler())
			{
				string value = GetConfigurationEntryValuesByGroupNameAndEntryName(groupName, entryName);
				cacheItem = Utility.ConvertTo<T>(value);
				Singletons.PutCacheItem(cacheKey, cacheItem);
			}

			return cacheItem;
		}

		public static T Get<T>(string groupName, string entryName, T defaultValue)
		{
			string cacheKey = $"Configuration:{groupName}:{entryName}:{defaultValue}:{(new StackTrace()).GetFrame(0).GetMethod().Name}";
			T cacheItem;
			if (Singletons.Cache.Contains(cacheKey))
			{
				cacheItem = Singletons.GetCacheItem<T>(cacheKey);
				if (cacheItem != null)
				{
					return cacheItem;
				}
			}

			using (var handler = new SqlConnectionHandler())
			{
				string value = GetConfigurationEntryValuesByGroupNameAndEntryName(groupName, entryName);

				if (value == null)
				{
					return defaultValue;
				}

				cacheItem = Utility.ConvertTo<T>(value);
				Singletons.PutCacheItem(cacheKey, cacheItem);
			}

			return cacheItem;
		}
	}
}
