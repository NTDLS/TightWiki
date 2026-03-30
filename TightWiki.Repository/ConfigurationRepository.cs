using NTDLS.Helpers;
using System.Data;
using System.Runtime.Caching;
using TightWiki.Plugin;
using TightWiki.Plugin.Caching;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Library;
using TightWiki.Plugin.Models;

namespace TightWiki.Repository
{
    public class ConfigurationRepository
        : ITwConfigurationRepository
    {
        public async Task<TwConfigurationEntries> GetConfigurationEntryValuesByGroupName(string groupName)
        {
            var cacheKey = TwCacheKeyFunction.Build(TwCache.Category.Configuration, [groupName]);

            return await TwCache.AddOrGet(cacheKey, async () =>
            {
                var entries = await ManagedDataStorage.Config.QueryAsync<TwConfigurationEntry>("GetConfigurationEntryValuesByGroupName.sql",
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

                return new TwConfigurationEntries(entries);
            }).EnsureNotNull();
        }

        public async Task<List<TwTheme>> GetAllThemes()
        {
            var cacheKey = TwCacheKeyFunction.Build(TwCache.Category.Configuration);

            return await TwCache.AddOrGet(cacheKey, async () =>
            {
                var themes = await ManagedDataStorage.Config.QueryAsync<TwTheme>("GetAllThemes.sql");

                foreach (var theme in themes)
                {
                    theme.Files = theme.DelimitedFiles.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
                }

                return themes;
            }).EnsureNotNull();
        }

        public async Task<TwWikiDatabaseStatistics> GetWikiDatabaseMetrics()
        {
            return await ManagedDataStorage.Config.EphemeralAsync(async o =>
            {
                using var users_db = o.Attach("users.db", "users_db");
                using var pages_db = o.Attach("pages.db", "pages_db");

                var result = await o.QuerySingleAsync<TwWikiDatabaseStatistics>("GetWikiDatabaseStatistics.sql");
                result.Exceptions = await LoggingRepository.GetExceptionCount();

                return result;
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
            var value = await ManagedDataStorage.Config.QueryFirstOrDefaultAsync<string>("GetCryptoCheck.sql") ?? string.Empty;

            try
            {
                value = Security.Helpers.DecryptString(Security.Helpers.MachineKey, value);
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
                Content = Security.Helpers.EncryptString(Security.Helpers.MachineKey, TwConstants.CRYPTOCHECK)
            };

            await ManagedDataStorage.Config.QueryFirstOrDefaultAsync<string>("SetCryptoCheck.sql", param);
        }

        public async Task SaveConfigurationEntryValueByGroupAndEntry(string groupName, string entryName, string value)
        {
            var param = new
            {
                GroupName = groupName,
                EntryName = entryName,
                Value = value
            };

            await ManagedDataStorage.Config.ExecuteAsync("SaveConfigurationEntryValueByGroupAndEntry.sql", param);
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
            => await ManagedDataStorage.Config.QueryAsync<TwConfigurationFlat>("GetFlatConfiguration.sql");

        public async Task<string?> GetConfigurationEntryValuesByGroupNameAndEntryName(string groupName, string entryName)
        {
            var cacheKey = TwCacheKeyFunction.Build(TwCache.Category.Configuration, [groupName, entryName]);

            return await TwCache.AddOrGetAsync(cacheKey, async () =>
            {
                var configEntry = await ManagedDataStorage.Config.QuerySingleAsync<TwConfigurationEntry>("GetConfigurationEntryValuesByGroupNameAndEntryName.sql",
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
            return await ManagedDataStorage.Config.QueryAsync<TwMenuItem>(query);
        }

        public async Task<TwMenuItem> GetMenuItemById(int id)
        {
            var param = new
            {
                Id = id
            };

            return await ManagedDataStorage.Config.QuerySingleAsync<TwMenuItem>("GetMenuItemById.sql", param);
        }

        public async Task DeleteMenuItemById(int id)
        {
            var param = new
            {
                Id = id
            };

            await ManagedDataStorage.Config.ExecuteAsync("DeleteMenuItemById.sql", param);

            TwCache.ClearCategory(TwCache.Category.Configuration);
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

            var menuItemId = await ManagedDataStorage.Config.ExecuteScalarAsync<int>("UpdateMenuItemById.sql", param);

            TwCache.ClearCategory(TwCache.Category.Configuration);
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

            var menuItemId = await ManagedDataStorage.Config.ExecuteScalarAsync<int>("InsertMenuItem.sql", param);

            TwCache.ClearCategory(TwCache.Category.Configuration);
            return menuItemId;
        }

        #endregion

        public async Task<List<TwEmoji>> ReloadEmojis(bool preloadAnimatedEmojis, int defaultEmojiHeight)
        {
            TwCache.ClearCategory(TwCache.Category.Emoji);
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
                            var imageCacheKey = TwCacheKey.Build(TwCache.Category.Emoji, [emoji.Shortcut]);
                            emoji.ImageData = (await EmojiRepository.GetEmojiByName(emoji.Name))?.ImageData;

                            if (emoji.ImageData != null)
                            {
                                var scaledImageCacheKey = TwCacheKey.Build(TwCache.Category.Emoji, [emoji.Shortcut, "100"]);
                                var decompressedImageBytes = Utility.Decompress(emoji.ImageData);
                                var img = SixLabors.ImageSharp.Image.Load(new MemoryStream(decompressedImageBytes));

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
                                var resized = TwImages.ResizeGifImage(decompressedImageBytes, Width, Height);
                                var itemCache = new TwImageCacheItem(resized, "image/gif");
                                TwCache.Set(scaledImageCacheKey, itemCache, new CacheItemPolicy());
                            }
                        }
                    });
                }).Start();
            }

            return emojis;
        }
    }
}
