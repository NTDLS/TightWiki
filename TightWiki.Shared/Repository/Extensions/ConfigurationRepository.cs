using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TightWiki.Shared.ADO;
using TightWiki.Shared.Library;
using TightWiki.Shared.Models.Data;

namespace TightWiki.Shared.Repository
{
    public static partial class ConfigurationRepository
    {
        public static List<Emoji>GetAllEmojis()
        {
            using var handler = new SqlConnectionHandler();
            return handler.Connection.Query<Emoji>("GetAllEmojis",
               null, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
        }

        public static WikiDatabaseStats GetWikiDatabaseStats()
        {
            using var handler = new SqlConnectionHandler();
            return handler.Connection.Query<WikiDatabaseStats>("GetWikiDatabaseStats",
                null, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
        }

        public static bool IsFirstRun(string content, string passphrase)
        {
            using var handler = new SqlConnectionHandler();
            var param = new
            {
                Content = content,
                Passphrase = passphrase
            };

            return handler.Connection.ExecuteScalar<bool>("IsFirstRun",
                param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
        }

        public static bool IsAdminPasswordDefault(string plainTextPassword)
        {
            using var handler = new SqlConnectionHandler();
            return handler.Connection.ExecuteScalar<bool>("IsAdminPasswordDefault",
                new { PlainTextPassword = plainTextPassword }, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
        }

        public static void SaveConfigurationEntryValueByGroupAndEntry(string groupName, string entryName, string value)
        {
            using var handler = new SqlConnectionHandler();
            var param = new
            {
                GroupName = groupName,
                EntryName = entryName,
                Value = value
            };

            handler.Connection.Execute("SaveConfigurationEntryValueByGroupAndEntry",
                param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
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
            using var handler = new SqlConnectionHandler();
            return handler.Connection.Query<ConfigurationFlat>("GetFlatConfiguration",
                null, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
        }

        public static ConfigurationEntries GetConfigurationEntryValuesByGroupName(string groupName, bool allowCache = true)
        {
            if (allowCache)
            {
                string cacheKey = $"Config:GetConfigurationEntryValuesByGroupName:{groupName}";
                var result = Cache.Get<ConfigurationEntries>(cacheKey);
                if (result == null)
                {
                    result = GetConfigurationEntryValuesByGroupName(groupName, false);
                    Cache.Put(cacheKey, result);
                }

                return result;
            }

            using (var handler = new SqlConnectionHandler())
            {
                var param = new
                {
                    GroupName = groupName
                };

                var result = handler.Connection.Query<ConfigurationEntry>("GetConfigurationEntryValuesByGroupName",
                    param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();

                foreach (var entry in result.Where(o => o.IsEncrypted))
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

                return new ConfigurationEntries(result);
            }
        }

        public static string GetConfigurationEntryValuesByGroupNameAndEntryName(string groupName, string entryName, bool allowCache = true)
        {
            if (allowCache)
            {
                string cacheKey = $"Config:GetConfigurationEntryValuesByGroupNameAndEntryName:{groupName}:{entryName}";
                var result = Cache.Get<string>(cacheKey);
                if (result == null)
                {
                    result = GetConfigurationEntryValuesByGroupNameAndEntryName(groupName, entryName, false);
                    Cache.Put(cacheKey, result);
                }

                return result;
            }

            using var handler = new SqlConnectionHandler();
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

            return configEntry?.Value?.ToString();
        }

        public static T Get<T>(string groupName, string entryName)
        {
            using var handler = new SqlConnectionHandler();
            string value = GetConfigurationEntryValuesByGroupNameAndEntryName(groupName, entryName);
            return Utility.ConvertTo<T>(value);
        }

        public static T Get<T>(string groupName, string entryName, T defaultValue)
        {
            using var handler = new SqlConnectionHandler();
            string value = GetConfigurationEntryValuesByGroupNameAndEntryName(groupName, entryName);

            if (value == null)
            {
                return defaultValue;
            }

            return Utility.ConvertTo<T>(value);
        }
    }
}
