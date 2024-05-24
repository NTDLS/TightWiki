using System.Data;
using TightWiki.Library;
using TightWiki.Models.DataModels;

namespace TightWiki.Repository
{
    public static class ConfigurationRepository
    {
        public static ConfigurationEntries GetConfigurationEntryValuesByGroupName(string groupName, bool allowCache = true)
        {
            var entries = ManagedDataStorage.Config.Query<ConfigurationEntry>
                ("GetConfigurationEntryValuesByGroupName.sql", new { GroupName = groupName }).ToList();

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
            return ManagedDataStorage.Config.Ephemeral(o =>
            {
                using var users_db = o.Attach("users.db", "users_db");
                using var pages_db = o.Attach("pages.db", "pages_db");

                var result = o.QuerySingle<WikiDatabaseStats>("GetWikiDatabaseStats.sql");
                result.Exceptions = ExceptionRepository.GetExceptionCount();

                return result;
            });
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
            var value = ManagedDataStorage.Config.QueryFirstOrDefault<string>("GetCryptoCheck.sql") ?? string.Empty;

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

            ManagedDataStorage.Config.QueryFirstOrDefault<string>("SetCryptoCheck.sql", param);
        }

        public static void SaveConfigurationEntryValueByGroupAndEntry(string groupName, string entryName, string value)
        {
            var param = new
            {
                GroupName = groupName,
                EntryName = entryName,
                Value = value
            };

            ManagedDataStorage.Config.Execute("SaveConfigurationEntryValueByGroupAndEntry.sql", param);

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
            return ManagedDataStorage.Config.Query<ConfigurationFlat>("GetFlatConfiguration.sql").ToList();
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

            var configEntry = ManagedDataStorage.Config.QuerySingle<ConfigurationEntry>("GetConfigurationEntryValuesByGroupNameAndEntryName.sql", param);
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

        #region Menu Items.

        public static List<MenuItem> GetAllMenuItems()
        {
            return ManagedDataStorage.Config.Query<MenuItem>("GetAllMenuItems.sql").ToList();
        }

        public static MenuItem GetMenuItemById(int id)
        {
            var param = new
            {
                Id = id
            };

            return ManagedDataStorage.Config.QuerySingle<MenuItem>("GetMenuItemById.sql", param);
        }

        public static void DeleteMenuItemById(int id)
        {
            var param = new
            {
                Id = id
            };

            ManagedDataStorage.Config.Execute("DeleteMenuItemById.sql", param);

            WikiCache.ClearCategory(WikiCache.Category.Configuration);
            GlobalSettings.MenuItems = GetAllMenuItems();
        }

        public static int UpdateMenuItemById(MenuItem menuItem)
        {
            var param = new
            {
                menuItem.Id,
                menuItem.Name,
                menuItem.Link,
                menuItem.Ordinal
            };

            var menuItemId = ManagedDataStorage.Config.ExecuteScalar<int>("UpdateMenuItemById.sql", param);

            WikiCache.ClearCategory(WikiCache.Category.Configuration);
            GlobalSettings.MenuItems = GetAllMenuItems();

            return menuItemId;
        }

        public static int InsertMenuItem(MenuItem menuItem)
        {
            var param = new
            {
                menuItem.Name,
                menuItem.Link,
                menuItem.Ordinal
            };

            var menuItemId = ManagedDataStorage.Config.ExecuteScalar<int>("InsertMenuItem.sql", param);

            WikiCache.ClearCategory(WikiCache.Category.Configuration);
            GlobalSettings.MenuItems = GetAllMenuItems();

            return menuItemId;
        }

        #endregion
    }
}
