using NTDLS.Helpers;
using SixLabors.ImageSharp;
using System.Data;
using System.Runtime.Caching;
using TightWiki.Caching;
using TightWiki.Library;
using TightWiki.Models.DataModels;

namespace TightWiki.Repository
{
    public static class ConfigurationRepository
    {
        public static async Task<ConfigurationEntries> GetConfigurationEntryValuesByGroupName(string groupName)
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Configuration, [groupName]);

            return await WikiCache.AddOrGet(cacheKey, async () =>
            {
                var entries = await ManagedDataStorage.Config.QueryAsync<ConfigurationEntry>("GetConfigurationEntryValuesByGroupName.sql",
                    new { GroupName = groupName });

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
            }).EnsureNotNull();
        }

        public static async Task<List<Theme>> GetAllThemes()
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Configuration);

            return await WikiCache.AddOrGet(cacheKey, async () =>
            {
                var themes = await ManagedDataStorage.Config.QueryAsync<Theme>("GetAllThemes.sql");

                foreach (var theme in themes)
                {
                    theme.Files = theme.DelimitedFiles.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
                }

                return themes;
            }).EnsureNotNull();
        }

        public static async Task<WikiDatabaseStatistics> GetWikiDatabaseMetrics()
        {
            return await ManagedDataStorage.Config.EphemeralAsync(async o =>
            {
                using var users_db = o.Attach("users.db", "users_db");
                using var pages_db = o.Attach("pages.db", "pages_db");

                var result = await o.QuerySingleAsync<WikiDatabaseStatistics>("GetWikiDatabaseStatistics.sql");
                result.Exceptions = await LoggingRepository.GetExceptionCount();

                return result;
            });
        }

        /// <summary>
        /// Determines if this is the first time the wiki has run. Returns true if it is the first time.
        /// </summary>
        public static async Task<bool> IsFirstRun()
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
        public static async Task<bool> GetCryptoCheck()
        {
            var value = await ManagedDataStorage.Config.QueryFirstOrDefaultAsync<string>("GetCryptoCheck.sql") ?? string.Empty;

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
        public static async Task SetCryptoCheck()
        {
            var param = new
            {
                Content = Security.Helpers.EncryptString(Security.Helpers.MachineKey, Constants.CRYPTOCHECK)
            };

            await ManagedDataStorage.Config.QueryFirstOrDefaultAsync<string>("SetCryptoCheck.sql", param);
        }

        public static async Task SaveConfigurationEntryValueByGroupAndEntry(string groupName, string entryName, string value)
        {
            var param = new
            {
                GroupName = groupName,
                EntryName = entryName,
                Value = value
            };

            await ManagedDataStorage.Config.ExecuteAsync("SaveConfigurationEntryValueByGroupAndEntry.sql", param);
        }

        public static async Task<List<ConfigurationNest>> GetConfigurationNest()
        {
            var result = new List<ConfigurationNest>();
            var flatConfig = await GetFlatConfiguration();

            var groups = flatConfig.GroupBy(o => o.GroupId);
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
                        DataType = value.DataType.ToLowerInvariant(),
                        IsEncrypted = value.IsEncrypted,
                        ConfigurationGroupId = group.Key,
                    });
                }
                result.Add(nest);
            }

            return result;
        }

        public static async Task<List<ConfigurationFlat>> GetFlatConfiguration()
            => await ManagedDataStorage.Config.QueryAsync<ConfigurationFlat>("GetFlatConfiguration.sql");

        public static async Task<string?> GetConfigurationEntryValuesByGroupNameAndEntryName(string groupName, string entryName)
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Configuration, [groupName, entryName]);

            return await WikiCache.AddOrGetAsync(cacheKey, async () =>
            {
                var configEntry = await ManagedDataStorage.Config.QuerySingleAsync<ConfigurationEntry>("GetConfigurationEntryValuesByGroupNameAndEntryName.sql",
                    new
                    {
                        GroupName = groupName,
                        EntryName = entryName
                    });

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
            });
        }

        public static async Task<T?> Get<T>(string groupName, string entryName)
        {
            var value = await GetConfigurationEntryValuesByGroupNameAndEntryName(groupName, entryName);
            return Converters.ConvertTo<T>(value.EnsureNotNull());
        }

        public static async Task<T> Get<T>(string groupName, string entryName, T defaultValue)
        {
            var value = await GetConfigurationEntryValuesByGroupNameAndEntryName(groupName, entryName);

            if (value == null)
            {
                return defaultValue;
            }

            return Converters.ConvertTo<T>(value);
        }

        #region Menu Items.

        public static async Task<List<MenuItem>> GetAllMenuItems(string? orderBy = null, string? orderByDirection = null)
        {
            var query = RepositoryHelpers.TransposeOrderby("GetAllMenuItems.sql", orderBy, orderByDirection);
            return await ManagedDataStorage.Config.QueryAsync<MenuItem>(query);
        }

        public static async Task<MenuItem> GetMenuItemById(int id)
        {
            var param = new
            {
                Id = id
            };

            return await ManagedDataStorage.Config.QuerySingleAsync<MenuItem>("GetMenuItemById.sql", param);
        }

        public static async Task DeleteMenuItemById(int id)
        {
            var param = new
            {
                Id = id
            };

            await ManagedDataStorage.Config.ExecuteAsync("DeleteMenuItemById.sql", param);

            WikiCache.ClearCategory(WikiCache.Category.Configuration);
        }

        public static async Task<int> UpdateMenuItemById(MenuItem menuItem)
        {
            var param = new
            {
                menuItem.Id,
                menuItem.Name,
                menuItem.Link,
                menuItem.Ordinal
            };

            var menuItemId = await ManagedDataStorage.Config.ExecuteScalarAsync<int>("UpdateMenuItemById.sql", param);

            WikiCache.ClearCategory(WikiCache.Category.Configuration);
            return menuItemId;
        }

        public static async Task<int> InsertMenuItem(MenuItem menuItem)
        {
            var param = new
            {
                menuItem.Name,
                menuItem.Link,
                menuItem.Ordinal
            };

            var menuItemId = await ManagedDataStorage.Config.ExecuteScalarAsync<int>("InsertMenuItem.sql", param);

            WikiCache.ClearCategory(WikiCache.Category.Configuration);
            return menuItemId;
        }

        #endregion

        public static async Task<List<Emoji>> ReloadEmojis(bool preloadAnimatedEmojis, int defaultEmojiHeight)
        {
            WikiCache.ClearCategory(WikiCache.Category.Emoji);
            var emojis = await EmojiRepository.GetAllEmojis();

            if (preloadAnimatedEmojis)
            {
                new Thread(async () =>
                {
                    var parallelOptions = new ParallelOptions
                    {
                        MaxDegreeOfParallelism = Environment.ProcessorCount / 2 < 2 ? 2 : Environment.ProcessorCount / 2
                    };

                    await Parallel.ForEachAsync(emojis, parallelOptions, async (emoji, cancellationToken) =>
                    {
                        if (emoji.MimeType.Equals("image/gif", StringComparison.InvariantCultureIgnoreCase))
                        {
                            var imageCacheKey = WikiCacheKey.Build(WikiCache.Category.Emoji, [emoji.Shortcut]);
                            emoji.ImageData = (await EmojiRepository.GetEmojiByName(emoji.Name))?.ImageData;

                            if (emoji.ImageData != null)
                            {
                                var scaledImageCacheKey = WikiCacheKey.Build(WikiCache.Category.Emoji, [emoji.Shortcut, "100"]);
                                var decompressedImageBytes = Utility.Decompress(emoji.ImageData);
                                var img = Image.Load(new MemoryStream(decompressedImageBytes));

                                int customScalePercent = 100;

                                var (Width, Height) = Utility.ScaleToMaxOf(img.Width, img.Height, defaultEmojiHeight);

                                //Adjust to any specified scaling.
                                Height = (int)(Height * (customScalePercent / 100.0));
                                Width = (int)(Width * (customScalePercent / 100.0));

                                //Adjusting by a ratio (and especially after applying additional scaling) may have caused one
                                //  dimension to become very small (or even negative). So here we will check the height and width
                                //  to ensure they are both at least n pixels and adjust both dimensions.
                                if (Height < 16)
                                {
                                    Height += 16 - Height;
                                    Width += 16 - Height;
                                }
                                if (Width < 16)
                                {
                                    Height += 16 - Width;
                                    Width += 16 - Width;
                                }

                                //These are hard to generate, so just keep it forever.
                                var resized = Images.ResizeGifImage(decompressedImageBytes, Width, Height);
                                var itemCache = new ImageCacheItem(resized, "image/gif");
                                WikiCache.Set(scaledImageCacheKey, itemCache, new CacheItemPolicy());
                            }
                        }
                    });
                }).Start();
            }

            return emojis;
        }
    }
}
