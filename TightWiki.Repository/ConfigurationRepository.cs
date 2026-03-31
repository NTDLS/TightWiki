using Microsoft.Extensions.Configuration;
using NTDLS.Helpers;
using NTDLS.SqliteDapperWrapper;
using System.Data;
using TightWiki.Library.Caching;
using TightWiki.Library.Extensions;
using TightWiki.Library.Security;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;
using TightWiki.Repository.Helpers;

namespace TightWiki.Repository
{
    public class ConfigurationRepository
        : ITwConfigurationRepository
    {
        public SqliteManagedFactory ConfigFactory { get; private set; }

        public ConfigurationRepository(IConfiguration configuration)
        {
            ConfigFactory = new SqliteManagedFactory(configuration.GetDatabaseConnectionString("ConfigConnection", "config.db"));
        }

        public async Task<TwConfigurationEntries> GetConfigurationEntryValuesByGroupName(string groupName)
        {
            var cacheKey = MemCacheKeyFunction.Build(MemCache.Category.Configuration, [groupName]);

            return await MemCache.AddOrGet(cacheKey, async () =>
            {
                var entries = await ConfigFactory.QueryAsync<TwConfigurationEntry>("GetConfigurationEntryValuesByGroupName.sql",
                    new { GroupName = groupName });

                foreach (var entry in entries)
                {
                    if (entry.IsEncrypted)
                    {
                        try
                        {
                            entry.Value = SecurityUtility.DecryptString(SecurityUtility.MachineKey, entry.Value);
                        }
                        catch
                        {
                            entry.Value = "";
                        }
                    }
                }

                return new TwConfigurationEntries(entries);
            }).EnsureNotNull();
        }

        public async Task<List<TwTheme>> GetAllThemes()
        {
            var cacheKey = MemCacheKeyFunction.Build(MemCache.Category.Configuration);

            return await MemCache.AddOrGet(cacheKey, async () =>
            {
                var themes = await ConfigFactory.QueryAsync<TwTheme>("GetAllThemes.sql");

                foreach (var theme in themes)
                {
                    theme.Files = theme.DelimitedFiles.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
                }

                return themes;
            }).EnsureNotNull();
        }

        public async Task<TwWikiDatabaseStatistics> GetWikiDatabaseMetrics()
        {
            return await ConfigFactory.EphemeralAsync(async o =>
            {
                using var users_db = o.Attach("users.db", "users_db");
                using var pages_db = o.Attach("pages.db", "pages_db");

                return await o.QuerySingleAsync<TwWikiDatabaseStatistics>("GetWikiDatabaseStatistics.sql");
            });
        }

        /// <summary>
        /// Determines if this is the first time the wiki has run. Returns true if it is the first time.
        /// </summary>
        public async Task<bool> IsFirstRun()
        {
            bool isEncryptionValid = await GetCryptoCheck();
            if (isEncryptionValid == false)
            {
                await SetCryptoCheck();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Reads an encrypted value from the database so we can determine if encryption is setup.
        /// If the value is missing then we are NOT setup.
        /// If the value is present but we cant decrypt it, then we are NOT setup.
        /// If the value is present and we can decrypt it, then we are setup and good to go!
        /// </summary>
        public async Task<bool> GetCryptoCheck()
        {
            var value = await ConfigFactory.QueryFirstOrDefaultAsync<string>("GetCryptoCheck.sql") ?? string.Empty;

            try
            {
                value = SecurityUtility.DecryptString(SecurityUtility.MachineKey, value);
                if (value == TwConstants.CRYPTOCHECK)
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
        public async Task SetCryptoCheck()
        {
            var param = new
            {
                Content = SecurityUtility.EncryptString(SecurityUtility.MachineKey, TwConstants.CRYPTOCHECK)
            };

            await ConfigFactory.QueryFirstOrDefaultAsync<string>("SetCryptoCheck.sql", param);
        }

        public async Task SaveConfigurationEntryValueByGroupAndEntry(string groupName, string entryName, string value)
        {
            var param = new
            {
                GroupName = groupName,
                EntryName = entryName,
                Value = value
            };

            await ConfigFactory.ExecuteAsync("SaveConfigurationEntryValueByGroupAndEntry.sql", param);
        }

        public async Task<List<TwConfigurationNest>> GetConfigurationNest()
        {
            var result = new List<TwConfigurationNest>();
            var flatConfig = await GetFlatConfiguration();

            var groups = flatConfig.GroupBy(o => o.GroupId);
            foreach (var group in groups)
            {
                var nest = new TwConfigurationNest
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
                            entryValue = SecurityUtility.DecryptString(SecurityUtility.MachineKey, value.EntryValue);
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

                    nest.Entries.Add(new TwConfigurationEntry()
                    {
                        Id = value.EntryId,
                        Value = entryValue,
                        Description = value.EntryDescription,
                        Name = value.EntryName,
                        DataType = value.DataType.ToLowerInvariant(),
                        IsEncrypted = value.IsEncrypted,
                        ConfigurationGroupId = group.Key,
                    });
                }
                result.Add(nest);
            }

            return result;
        }

        public async Task<List<TwConfigurationFlat>> GetFlatConfiguration()
            => await ConfigFactory.QueryAsync<TwConfigurationFlat>("GetFlatConfiguration.sql");

        public async Task<string?> GetConfigurationEntryValuesByGroupNameAndEntryName(string groupName, string entryName)
        {
            var cacheKey = MemCacheKeyFunction.Build(MemCache.Category.Configuration, [groupName, entryName]);

            return await MemCache.AddOrGetAsync(cacheKey, async () =>
            {
                var configEntry = await ConfigFactory.QuerySingleAsync<TwConfigurationEntry>("GetConfigurationEntryValuesByGroupNameAndEntryName.sql",
                    new
                    {
                        GroupName = groupName,
                        EntryName = entryName
                    });

                if (configEntry?.IsEncrypted == true)
                {
                    try
                    {
                        configEntry.Value = SecurityUtility.DecryptString(SecurityUtility.MachineKey, configEntry.Value);
                    }
                    catch
                    {
                        configEntry.Value = "";
                    }
                }

                return configEntry?.Value?.ToString();
            });
        }

        public async Task<T?> Get<T>(string groupName, string entryName)
        {
            var value = await GetConfigurationEntryValuesByGroupNameAndEntryName(groupName, entryName);
            return Converters.ConvertTo<T>(value.EnsureNotNull());
        }

        public async Task<T> Get<T>(string groupName, string entryName, T defaultValue)
        {
            var value = await GetConfigurationEntryValuesByGroupNameAndEntryName(groupName, entryName);

            if (value == null)
            {
                return defaultValue;
            }

            return Converters.ConvertTo<T>(value);
        }

        #region Menu Items.

        public async Task<List<TwMenuItem>> GetAllMenuItems(string? orderBy = null, string? orderByDirection = null)
        {
            var query = RepositoryHelpers.TransposeOrderby("GetAllMenuItems.sql", orderBy, orderByDirection);
            return await ConfigFactory.QueryAsync<TwMenuItem>(query);
        }

        public async Task<TwMenuItem> GetMenuItemById(int id)
        {
            var param = new
            {
                Id = id
            };

            return await ConfigFactory.QuerySingleAsync<TwMenuItem>("GetMenuItemById.sql", param);
        }

        public async Task DeleteMenuItemById(int id)
        {
            var param = new
            {
                Id = id
            };

            await ConfigFactory.ExecuteAsync("DeleteMenuItemById.sql", param);

            MemCache.ClearCategory(MemCache.Category.Configuration);
        }

        public async Task<int> UpdateMenuItemById(TwMenuItem menuItem)
        {
            var param = new
            {
                menuItem.Id,
                menuItem.Name,
                menuItem.Link,
                menuItem.Ordinal
            };

            var menuItemId = await ConfigFactory.ExecuteScalarAsync<int>("UpdateMenuItemById.sql", param);

            MemCache.ClearCategory(MemCache.Category.Configuration);
            return menuItemId;
        }

        public async Task<int> InsertMenuItem(TwMenuItem menuItem)
        {
            var param = new
            {
                menuItem.Name,
                menuItem.Link,
                menuItem.Ordinal
            };

            var menuItemId = await ConfigFactory.ExecuteScalarAsync<int>("InsertMenuItem.sql", param);

            MemCache.ClearCategory(MemCache.Category.Configuration);
            return menuItemId;
        }

        #endregion
    }
}
