using System.Data;
using TightWiki.Caching;
using TightWiki.Configuration;
using TightWiki.Library;
using TightWiki.Models.DataModels;

namespace TightWiki.Repository
{
    public static class ConfigurationRepository
    {
        public static ConfigurationEntries GetConfigurationEntryValuesByGroupName(string groupName, bool allowCache = true)
        {
            if (allowCache)
            {
                var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Configuration, [groupName]);
                if (!WikiCache.TryGet<ConfigurationEntries>(cacheKey, out var result))
                {
                    result = GetConfigurationEntryValuesByGroupName(groupName, false);
                    WikiCache.Put(cacheKey, result);
                }

                return result;
            }

            var entries = ManagedDataStorage.Config.Query<ConfigurationEntry>
                ("GetConfigurationEntryValuesByGroupName.sql", new { GroupName = groupName }).ToList();

            foreach (var entry in entries)
            {
                if (entry.IsEncrypted)
                {
                    try
                    {
                        entry.Value = Security.Helpers.DecryptString(Security.Helpers.MachineKey, entry.Value);
                    }
                    catch
                    {
                        entry.Value = "";
                    }
                }
            }

            return new ConfigurationEntries(entries);
        }

        public static List<Theme> GetAllThemes()
        {
            var collection = ManagedDataStorage.Config.Query<Theme>("GetAllThemes.sql").ToList();

            foreach (var theme in collection)
            {
                theme.Files = theme.DelimitedFiles.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            return collection;
        }

        public static WikiDatabaseStatistics GetWikiDatabaseMetrics()
        {
            return ManagedDataStorage.Config.Ephemeral(o =>
            {
                using var users_db = o.Attach("users.db", "users_db");
                using var pages_db = o.Attach("pages.db", "pages_db");

                var result = o.QuerySingle<WikiDatabaseStatistics>("GetWikiDatabaseStatistics.sql");
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
        /// Reads an encrypted value from the database so we can determine if encryption is setup.
        /// If the value is missing then we are NOT setup.
        /// If the value is present but we cant decrypt it, then we are NOT setup.
        /// /// If the value is present and we can decrypt it, then we are setup and good to go!
        /// </summary>
        /// <returns></returns>
        public static bool GetCryptoCheck()
        {
            var value = ManagedDataStorage.Config.QueryFirstOrDefault<string>("GetCryptoCheck.sql") ?? string.Empty;

            try
            {
                value = Security.Helpers.DecryptString(Security.Helpers.MachineKey, value);
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
        /// Writes an encrypted value to the database so we can test at a later time to ensure that encryption is setup.
        /// </summary>
        public static void SetCryptoCheck()
        {
            var param = new
            {
                Content = Security.Helpers.EncryptString(Security.Helpers.MachineKey, Constants.CRYPTOCHECK)
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

            GlobalConfiguration.ReloadEverything();
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
                            entryValue = Security.Helpers.DecryptString(Security.Helpers.MachineKey, value.EntryValue);
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
            => ManagedDataStorage.Config.Query<ConfigurationFlat>("GetFlatConfiguration.sql").ToList();

        public static string? GetConfigurationEntryValuesByGroupNameAndEntryName(string groupName, string entryName, bool allowCache = true)
        {
            if (allowCache)
            {
                var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Configuration, [groupName, entryName]);
                if (!WikiCache.TryGet<string>(cacheKey, out var result))
                {
                    if ((result = GetConfigurationEntryValuesByGroupNameAndEntryName(groupName, entryName, false)) != null)
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
                    configEntry.Value = Security.Helpers.DecryptString(Security.Helpers.MachineKey, configEntry.Value);
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
            => ManagedDataStorage.Config.Query<MenuItem>("GetAllMenuItems.sql").ToList();

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
