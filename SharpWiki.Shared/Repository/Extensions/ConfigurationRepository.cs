using SharpWiki.Shared.ADO;
using SharpWiki.Shared.Library;
using SharpWiki.Shared.Models;
using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;

namespace SharpWiki.Shared.Repository
{
    public static partial class ConfigurationRepository
	{
		public static void SaveConfigurationEntryValueByGroupAndEntry(string groupName, string entryName, string value)
		{
			Singletons.ClearCacheItems("Configuration");

			using (var handler = new SqlConnectionHandler())
			{
				var param = new
				{
					GroupName = groupName,
					EntryName = entryName,
					Value = value
				};

				handler.Connection.Execute("SaveConfigurationEntryValueByGroupAndEntry",
					param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
			}
		}

		public static List<ConfigurationNest> GetConfigurationNest()
		{
			var result = new List<ConfigurationNest>();
			var flatConfig = GetFlatConfiguration();

			var groups = flatConfig.GroupBy(o => o.GroupId).OrderBy(o => o.Key).ToList();
			foreach (var group in groups)
			{
				var nest = new ConfigurationNest
				{
					Id = group.Key,
					Name = group.Select(o => o.GroupName).First(),
					Description = group.Select(o => o.GroupDescription).First()
				};

				foreach (var value in group.OrderBy(o => o.EntryName))
				{
					string entryValue;
					if (value.IsEncrypted)
					{
						try
						{
							entryValue = Security.DecryptString(Security.MachineKey, value.EntryValue);
						}
						catch
						{
							entryValue = "";
						}
					}
					else
					{
						entryValue = value.EntryValue;
					}

					nest.Entries.Add(new ConfigurationEntry()
					{
						Id = value.EntryId,
						Value = entryValue,
						Description = value.EntryDescription,
						Name = value.EntryName,
						DataType = value.DataType.ToLower(),
						IsEncrypted = value.IsEncrypted,
						ConfigurationGroupId = group.Key,
					});
				}
				result.Add(nest);
			}

			return result;
		}

		public static List<ConfigurationFlat> GetFlatConfiguration()
		{
			using (var handler = new SqlConnectionHandler())
			{
				return handler.Connection.Query<ConfigurationFlat>("GetFlatConfiguration",
					null, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
			}
		}

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

				foreach (var entry in cacheItem.Where(o => o.IsEncrypted))
				{
					try
					{
						entry.Value = Security.DecryptString(Security.MachineKey, entry.Value);
					}
					catch
					{
						entry.Value = "";
					}
				}

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

				var configEntry = handler.Connection.Query<ConfigurationEntry>("GetConfigurationEntryValuesByGroupNameAndEntryName",
					param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();

				if (configEntry?.IsEncrypted == true)
				{
					try
					{
						configEntry.Value = Security.DecryptString(Security.MachineKey, configEntry.Value);
					}
					catch
					{
						configEntry.Value = "";
					}
				}

				cacheItem = configEntry?.Value?.ToString();

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
