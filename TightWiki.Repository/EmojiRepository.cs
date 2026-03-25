using TightWiki.Library;
using TightWiki.Models;
using TightWiki.Models.DataModels;

namespace TightWiki.Repository
{
    public static partial class EmojiRepository
    {
        public static async Task< List<Emoji>> GetAllEmojis()
            => await ManagedDataStorage.Emoji.QueryAsync<Emoji>("GetAllEmojis.sql");

        public static async Task<IEnumerable<string>> AutoCompleteEmoji(string term)
            => await ManagedDataStorage.Emoji.QueryAsync<string>("AutoCompleteEmoji.sql", new { Term = term });
        public static async Task<IEnumerable<Emoji>> GetEmojisByCategory(string category)
            => await ManagedDataStorage.Emoji.QueryAsync<Emoji>("GetEmojisByCategory.sql", new { Category = category });

        public static async Task<IEnumerable<EmojiCategory>> GetEmojiCategoriesGrouped()
            => await ManagedDataStorage.Emoji.QueryAsync<EmojiCategory>("GetEmojiCategoriesGrouped.sql");

        public static async Task<IEnumerable<int>> SearchEmojiCategoryIds(List<string> categories)
        {
            return await ManagedDataStorage.Emoji.EphemeralAsync(async o =>
            {
                var param = new
                {
                    SearchTokenCount = categories.Count
                };

                using var tempTable = o.CreateTempTableFrom("TempCategories", categories);
                return await o.QueryAsync<int>("SearchEmojiCategoryIds.sql", param);
            });
        }

        public static async Task<List<EmojiCategory>> GetEmojiCategoriesByName(string name)
        {
            var param = new
            {
                Name = name
            };

            return await ManagedDataStorage.Emoji.QueryAsync<EmojiCategory>("GetEmojiCategoriesByName.sql", param);
        }

        public static async Task DeleteById(int id)
        {
            var param = new
            {
                Id = id
            };

            await ManagedDataStorage.Emoji.ExecuteAsync("DeleteEmojiById.sql", param);

            await ConfigurationRepository.ReloadEmojis();
        }

        public static async Task<Emoji?> GetEmojiByName(string name)
        {
            var param = new
            {
                Name = name
            };

            return await ManagedDataStorage.Emoji.QuerySingleOrDefaultAsync<Emoji>("GetEmojiByName.sql", param);
        }

        public static async Task<int> UpsertEmoji(UpsertEmoji emoji)
        {
            int emojiId = await ManagedDataStorage.Emoji.EphemeralAsync(async o =>
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

            await ConfigurationRepository.ReloadEmojis();

            return emojiId;
        }

        public static async Task<List<Emoji>> GetAllEmojisPaged(int pageNumber,
            string? orderBy = null, string? orderByDirection = null, List<string>? categories = null)
        {
            if (categories == null || categories.Count == 0)
            {
                var param = new
                {
                    PageNumber = pageNumber,
                    PageSize = GlobalConfiguration.PaginationSize
                };

                var query = RepositoryHelper.TransposeOrderby("GetAllEmojisPaged.sql", orderBy, orderByDirection);
                return await ManagedDataStorage.Emoji.QueryAsync<Emoji>(query, param);
            }
            else
            {
                var emojiCategoryIds = await SearchEmojiCategoryIds(categories);
                var param = new
                {
                    PageNumber = pageNumber,
                    PageSize = GlobalConfiguration.PaginationSize
                };

                return await ManagedDataStorage.Emoji.EphemeralAsync(async o =>
                {
                    var getAllEmojisPagedByCategoriesParam = new
                    {
                        SearchTokenCount = emojiCategoryIds.Count(),
                        PageNumber = pageNumber,
                        PageSize = GlobalConfiguration.PaginationSize
                    };

                    using var tempTable = o.CreateTempTableFrom("TempEmojiCategoryIds", emojiCategoryIds);

                    var query = RepositoryHelper.TransposeOrderby("GetAllEmojisPagedByCategories.sql", orderBy, orderByDirection);
                    return await o.QueryAsync<Emoji>(query, getAllEmojisPagedByCategoriesParam);
                });
            }
        }
    }
}
