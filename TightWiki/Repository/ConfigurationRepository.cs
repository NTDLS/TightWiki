using System.Data;
using TightWiki.DataStorage;
using TightWiki.Library;
using TightWiki.Models.DataModels;

namespace TightWiki.Repository
{
    public static class ConfigurationRepository
    {
        public static ConfigurationEntries GetConfigurationEntryValuesByGroupName(string groupName, bool allowCache = true)
        {
            var entries = ManagedDataStorage.Default.Query<ConfigurationEntry>
                ("GetConfigurationEntryValuesByGroupName", new { GroupName = groupName }).ToList();

            foreach (var entry in entries)
            {
                if (entry.IsEncrypted)
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
            }

            return new ConfigurationEntries(entries);
        }

        public static WikiDatabaseStats GetWikiDatabaseStats()
        {
            var stats = ManagedDataStorage.Default.QuerySingle<WikiDatabaseStats>("GetWikiDatabaseStats");
            stats.Exceptions = ExceptionRepository.GetExceptionCount();
            return stats;
        }

        /// <summary>
        /// Determines if this is the first time the wiki has run. Returns true if it is the first time.
        /// </summary>
        /// <returns></returns>
        public static bool IsFirstRun()
        {
            bool isEncryptionValid = GetCryptoCheck();
            if (isEncryptionValid == false)
            {
                SetCryptoCheck();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Reads an encryptes value from the database so we can determine if encryption is setup.
        /// </summary>
        /// <returns></returns>
        public static bool GetCryptoCheck()
        {
            var value = ManagedDataStorage.Default.QueryFirstOrDefault<string>("GetCryptoCheck") ?? string.Empty;

            try
            {
                value = Security.DecryptString(Security.MachineKey, value);
                if (value == Constants.CRYPTOCHECK)
                {
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        /// <summary>
        /// Writes an encrypted value to the database so we can test it later.
        /// </summary>
        public static void SetCryptoCheck()
        {
            var param = new
            {
                Content = Security.EncryptString(Security.MachineKey, Constants.CRYPTOCHECK)
            };

            ManagedDataStorage.Default.QueryFirstOrDefault<string>("SetCryptoCheck", param);
        }

        public static bool IsAdminPasswordChanged()
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Configuration);

            if (WikiCache.Get<bool?>(cacheKey) == true)
            {
                return true;
            }

            var result = ManagedDataStorage.Default.ExecuteScalar<bool?>("IsAdminPasswordChanged");
            if (result == true)
            {
                WikiCache.Put(cacheKey, true);
                return true;
            }

            return false;
        }

        public static void SetAdminPasswordIsChanged()
        {
            ManagedDataStorage.Default.ExecuteScalar<bool>("SetAdminPasswordIsChanged");
        }

        public static void SetAdminPasswordIsDefault()
        {
            ManagedDataStorage.Default.ExecuteScalar<bool>("SetAdminPasswordIsDefault");
        }

        public static void SaveConfigurationEntryValueByGroupAndEntry(string groupName, string entryName, string value)
        {
            var param = new
            {
                GroupName = groupName,
                EntryName = entryName,
                Value = value
            };

            ManagedDataStorage.Default.Execute("SaveConfigurationEntryValueByGroupAndEntry", param);

            GlobalSettings.ReloadEverything();
        }

        public static List<ConfigurationNest> GetConfigurationNest()
        {
            var result = new List<ConfigurationNest>();
            var flatConfig = GetFlatConfiguration();

            var groups = flatConfig.GroupBy(o => o.GroupId).ToList();
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
            return ManagedDataStorage.Default.Query<ConfigurationFlat>("GetFlatConfiguration").ToList();
        }

        public static string? GetConfigurationEntryValuesByGroupNameAndEntryName(string groupName, string entryName, bool allowCache = true)
        {
            if (allowCache)
            {
                var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Configuration, [groupName, entryName]);
                var result = WikiCache.Get<string>(cacheKey);
                if (result == null)
                {
                    result = GetConfigurationEntryValuesByGroupNameAndEntryName(groupName, entryName, false);
                    if (result != null)
                    {
                        WikiCache.Put(cacheKey, result);
                    }
                }

                return result;
            }

            var param = new
            {
                GroupName = groupName,
                EntryName = entryName
            };

            var configEntry = ManagedDataStorage.Default.QuerySingle<ConfigurationEntry>("GetConfigurationEntryValuesByGroupNameAndEntryName", param);
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

        public static T? Get<T>(string groupName, string entryName)
        {
            var value = GetConfigurationEntryValuesByGroupNameAndEntryName(groupName, entryName);
            return Utility.ConvertTo<T>(value);
        }

        public static T? Get<T>(string groupName, string entryName, T defaultValue)
        {
            var value = GetConfigurationEntryValuesByGroupNameAndEntryName(groupName, entryName);

            if (value == null)
            {
                return defaultValue;
            }

            return Utility.ConvertTo<T>(value);
        }
    }
}
