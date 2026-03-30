using Microsoft.Extensions.Configuration;
using NTDLS.SqliteDapperWrapper;
using System.Runtime.Caching;
using TightWiki.Plugin.Caching;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Library;
using TightWiki.Plugin.Models;
using TightWiki.Repository.Extensions;
using TightWiki.Repository.Helpers;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Repository
{
    public partial class EmojiRepository
        : ITwEmojiRepository
    {
        readonly private ITwConfigurationRepository _configurationRepository;
        public SqliteManagedFactory EmojiFactory { get; private set; }

        public EmojiRepository(IConfiguration configuration, ITwConfigurationRepository configurationRepository)
        {
            _configurationRepository = configurationRepository;
            var configDatabaseFile = configurationRepository.ConfigFactory.Ephemeral(o => o.NativeConnection.DataSource);
            var connectionString = configuration.GetDatabaseConnectionString("EmojiConnection", "emoji.db", configDatabaseFile);
            EmojiFactory = new SqliteManagedFactory(connectionString);
        }

        public async Task<List<TwEmoji>> GetAllEmojis()
            => await EmojiFactory.QueryAsync<TwEmoji>("GetAllEmojis.sql");

        public async Task<List<string>> AutoCompleteEmoji(string term)
            => await EmojiFactory.QueryAsync<string>("AutoCompleteEmoji.sql", new { Term = term });
        public async Task<List<TwEmoji>> GetEmojisByCategory(string category)
            => await EmojiFactory.QueryAsync<TwEmoji>("GetEmojisByCategory.sql", new { Category = category });

        public async Task<List<TwEmojiCategory>> GetEmojiCategoriesGrouped()
            => await EmojiFactory.QueryAsync<TwEmojiCategory>("GetEmojiCategoriesGrouped.sql");

        public async Task<List<int>> SearchEmojiCategoryIds(List<string> categories)
        {
            return await EmojiFactory.EphemeralAsync(async o =>
            {
                var param = new
                {
                    SearchTokenCount = categories.Count
                };

                using var tempTable = o.CreateTempTableFrom("TempCategories", categories);
                return await o.QueryAsync<int>("SearchEmojiCategoryIds.sql", param);
            });
        }

        public async Task<List<TwEmojiCategory>> GetEmojiCategoriesByName(string name)
        {
            var param = new
            {
                Name = name
            };

            return await EmojiFactory.QueryAsync<TwEmojiCategory>("GetEmojiCategoriesByName.sql", param);
        }

        public async Task DeleteById(int id)
        {
            var param = new
            {
                Id = id
            };

            await EmojiFactory.ExecuteAsync("DeleteEmojiById.sql", param);
        }

        public async Task<TwEmoji?> GetEmojiByName(string name)
        {
            var param = new
            {
                Name = name
            };

            return await EmojiFactory.QuerySingleOrDefaultAsync<TwEmoji>("GetEmojiByName.sql", param);
        }

        public async Task<int> UpsertEmoji(TwUpsertEmoji emoji)
        {
            int emojiId = await EmojiFactory.EphemeralAsync(async o =>
            {
                var transaction = o.BeginTransaction();

                try
                {
                    if (emoji.Id == null || emoji.Id == 0)
                    {
                        var param = new
                        {
                            Name = emoji.Name,
                            ImageData = emoji.ImageData == null ? null : Utility.Compress(emoji.ImageData),
                            MimeType = emoji.MimeType
                        };
                        emoji.Id = await o.ExecuteScalarAsync<int>("InsertEmoji.sql", param);
                    }
                    else
                    {
                        var param = new
                        {
                            EmojiId = emoji.Id,
                            Name = emoji.Name,
                            ImageData = emoji.ImageData == null ? null : Utility.Compress(emoji.ImageData),
                            MimeType = emoji.MimeType
                        };
                        await o.ExecuteScalarAsync<int>("UpdateEmoji.sql", param);
                    }

                    var upsertEmojiCategoriesParam = new
                    {
                        EmojiId = emoji.Id
                    };

                    using var tempTable = o.CreateTempTableFrom("TempEmojiCategories", emoji.Categories, transaction);
                    await o.ExecuteAsync("UpsertEmojiCategories.sql", upsertEmojiCategoriesParam);

                    transaction.Commit();

                    return (int)emoji.Id;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            });

            return emojiId;
        }

        public async Task<List<TwEmoji>> GetAllEmojisPaged(int pageNumber,
            string? orderBy = null, string? orderByDirection = null, List<string>? categories = null)
        {
            var paginationSize = await _configurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            if (categories == null || categories.Count == 0)
            {
                var param = new
                {
                    PageNumber = pageNumber,
                    PageSize = paginationSize
                };

                var query = RepositoryHelpers.TransposeOrderby("GetAllEmojisPaged.sql", orderBy, orderByDirection);
                return await EmojiFactory.QueryAsync<TwEmoji>(query, param);
            }
            else
            {
                var emojiCategoryIds = await SearchEmojiCategoryIds(categories);
                var param = new
                {
                    PageNumber = pageNumber,
                    PageSize = paginationSize
                };

                return await EmojiFactory.EphemeralAsync(async o =>
                {
                    var getAllEmojisPagedByCategoriesParam = new
                    {
                        SearchTokenCount = emojiCategoryIds.Count(),
                        PageNumber = pageNumber,
                        PageSize = paginationSize
                    };

                    using var tempTable = o.CreateTempTableFrom("TempEmojiCategoryIds", emojiCategoryIds);

                    var query = RepositoryHelpers.TransposeOrderby("GetAllEmojisPagedByCategories.sql", orderBy, orderByDirection);
                    return await o.QueryAsync<TwEmoji>(query, getAllEmojisPagedByCategoriesParam);
                });
            }
        }

        public async Task<List<TwEmoji>> ReloadEmojis(bool preloadAnimatedEmojis, int defaultEmojiHeight)
        {
            TwCache.ClearCategory(TwCache.Category.Emoji);
            var emojis = await GetAllEmojis();

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
                            emoji.ImageData = (await GetEmojiByName(emoji.Name))?.ImageData;

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
