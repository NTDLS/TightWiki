using NTDLS.Helpers;
using SixLabors.ImageSharp;
using System.Data;
using System.Diagnostics;
using System.Runtime.Caching;
using TightWiki.Caching;
using TightWiki.Library;
using TightWiki.Models;
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

            await ReloadEverything();
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
            var query = RepositoryHelper.TransposeOrderby("GetAllMenuItems.sql", orderBy, orderByDirection);
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
            GlobalConfiguration.MenuItems = await GetAllMenuItems();
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
            GlobalConfiguration.MenuItems = await GetAllMenuItems();

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
            GlobalConfiguration.MenuItems = await GetAllMenuItems();

            return menuItemId;
        }

        #endregion

        public static async Task ReloadEmojis()
        {
            WikiCache.ClearCategory(WikiCache.Category.Emoji);
            GlobalConfiguration.Emojis = await EmojiRepository.GetAllEmojis();

            if (GlobalConfiguration.PreLoadAnimatedEmojis)
            {
                new Thread(async () =>
                {
                    var parallelOptions = new ParallelOptions
                    {
                        MaxDegreeOfParallelism = Environment.ProcessorCount / 2 < 2 ? 2 : Environment.ProcessorCount / 2
                    };

                    await Parallel.ForEachAsync(GlobalConfiguration.Emojis, parallelOptions, async (emoji, cancellationToken) =>
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

                                var (Width, Height) = Utility.ScaleToMaxOf(img.Width, img.Height, GlobalConfiguration.DefaultEmojiHeight);

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
        }

        public static async Task ReloadEverything()
        {
            WikiCache.Clear();

            GlobalConfiguration.IsDebug = Debugger.IsAttached;

            var performanceConfig = await GetConfigurationEntryValuesByGroupName(Constants.WikiConfigurationGroup.Performance);
            GlobalConfiguration.PageCacheSeconds = performanceConfig.Value<int>("Page Cache Time (Seconds)");
            GlobalConfiguration.RecordCompilationMetrics = performanceConfig.Value<bool>("Record Compilation Metrics");
            GlobalConfiguration.CacheMemoryLimitMB = performanceConfig.Value<int>("Cache Memory Limit MB");

            WikiCache.Initialize(GlobalConfiguration.CacheMemoryLimitMB, TimeSpan.FromSeconds(GlobalConfiguration.PageCacheSeconds));

            var basicConfig = await GetConfigurationEntryValuesByGroupName(Constants.WikiConfigurationGroup.Basic);
            var customizationConfig = await GetConfigurationEntryValuesByGroupName(Constants.WikiConfigurationGroup.Customization);
            var htmlConfig = await GetConfigurationEntryValuesByGroupName(Constants.WikiConfigurationGroup.HTMLLayout);
            var functionalityConfig = await GetConfigurationEntryValuesByGroupName(Constants.WikiConfigurationGroup.Functionality);
            var membershipConfig = await GetConfigurationEntryValuesByGroupName(Constants.WikiConfigurationGroup.Membership);
            var searchConfig = await GetConfigurationEntryValuesByGroupName(Constants.WikiConfigurationGroup.Search);
            var filesAndAttachmentsConfig = await GetConfigurationEntryValuesByGroupName(Constants.WikiConfigurationGroup.FilesAndAttachments);
            var ldapAuthentication = await GetConfigurationEntryValuesByGroupName(Constants.WikiConfigurationGroup.LDAPAuthentication);
            GlobalConfiguration.EnableLDAPAuthentication = ldapAuthentication.Value("LDAP : Enable LDAP Authentication", false);

            GlobalConfiguration.Address = basicConfig?.Value<string>("Address") ?? string.Empty;
            GlobalConfiguration.Name = basicConfig?.Value<string>("Name") ?? string.Empty;
            GlobalConfiguration.Copyright = basicConfig?.Value<string>("Copyright") ?? string.Empty;

            var themeName = customizationConfig.Value("Theme", "Light");

            GlobalConfiguration.FixedMenuPosition = customizationConfig.Value("Fixed Header Menu Position", false);
            GlobalConfiguration.AllowSignup = membershipConfig.Value("Allow Signup", false);
            GlobalConfiguration.DefaultProfileRecentlyModifiedCount = performanceConfig.Value<int>("Default Profile Recently Modified Count");
            GlobalConfiguration.PreLoadAnimatedEmojis = performanceConfig.Value<bool>("Pre-Load Animated Emojis");
            GlobalConfiguration.SystemTheme = (await GetAllThemes()).Single(o => o.Name == themeName);
            GlobalConfiguration.DefaultEmojiHeight = customizationConfig.Value<int>("Default Emoji Height");
            GlobalConfiguration.PaginationSize = customizationConfig.Value<int>("Pagination Size");

            GlobalConfiguration.DefaultTimeZone = customizationConfig?.Value<string>("Default TimeZone") ?? string.Empty;
            GlobalConfiguration.IncludeWikiDescriptionInMeta = functionalityConfig.Value<bool>("Include wiki Description in Meta");
            GlobalConfiguration.IncludeWikiTagsInMeta = functionalityConfig.Value<bool>("Include wiki Tags in Meta");
            GlobalConfiguration.EnablePageComments = functionalityConfig.Value<bool>("Enable Page Comments");
            GlobalConfiguration.EnablePublicProfiles = functionalityConfig.Value<bool>("Enable Public Profiles");
            GlobalConfiguration.ShowCommentsOnPageFooter = functionalityConfig.Value<bool>("Show Comments on Page Footer");
            GlobalConfiguration.ShowChangeSummaryWhenEditing = functionalityConfig.Value<bool>("Show Change Summary when Editing");
            GlobalConfiguration.RequireChangeSummaryWhenEditing = functionalityConfig.Value<bool>("Require Change Summary when Editing");
            GlobalConfiguration.ShowLastModifiedOnPageFooter = functionalityConfig.Value<bool>("Show Last Modified on Page Footer");
            GlobalConfiguration.IncludeSearchOnNavbar = searchConfig.Value<bool>("Include Search on Navbar");
            GlobalConfiguration.HTMLHeader = htmlConfig?.Value<string>("Header") ?? string.Empty;
            GlobalConfiguration.HTMLFooter = htmlConfig?.Value<string>("Footer") ?? string.Empty;
            GlobalConfiguration.HTMLPreBody = htmlConfig?.Value<string>("Pre-Body") ?? string.Empty;
            GlobalConfiguration.HTMLPostBody = htmlConfig?.Value<string>("Post-Body") ?? string.Empty;
            GlobalConfiguration.BrandImageSmall = customizationConfig?.Value<string>("Brand Image (Small)") ?? string.Empty;
            GlobalConfiguration.FooterBlurb = customizationConfig?.Value<string>("FooterBlurb") ?? string.Empty;
            GlobalConfiguration.MaxAvatarFileSize = filesAndAttachmentsConfig.Value<int>("Max Avatar File Size");
            GlobalConfiguration.MaxAttachmentFileSize = filesAndAttachmentsConfig.Value<int>("Max Attachment File Size");
            GlobalConfiguration.MaxEmojiFileSize = filesAndAttachmentsConfig.Value<int>("Max Emoji File Size");

            GlobalConfiguration.MenuItems = await GetAllMenuItems();

            await ReloadEmojis();
        }
    }
}
