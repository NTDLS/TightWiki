using TightWiki.Library;
using TightWiki.Models.DataModels;
using TightWiki.Plugin.Models;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Repository
{
    public static partial class EmojiRepository
    {
        public static async Task<List<TwEmoji>> GetAllEmojis()
            => await ManagedDataStorage.Emoji.QueryAsync<TwEmoji>("GetAllEmojis.sql");

        public static async Task<List<string>> AutoCompleteEmoji(string term)
            => await ManagedDataStorage.Emoji.QueryAsync<string>("AutoCompleteEmoji.sql", new { Term = term });
        public static async Task<List<TwEmoji>> GetEmojisByCategory(string category)
            => await ManagedDataStorage.Emoji.QueryAsync<TwEmoji>("GetEmojisByCategory.sql", new { Category = category });

        public static async Task<List<TwEmojiCategory>> GetEmojiCategoriesGrouped()
            => await ManagedDataStorage.Emoji.QueryAsync<TwEmojiCategory>("GetEmojiCategoriesGrouped.sql");

        public static async Task<List<int>> SearchEmojiCategoryIds(List<string> categories)
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

        public static async Task<List<TwEmojiCategory>> GetEmojiCategoriesByName(string name)
        {
            var param = new
            {
                Name = name
            };

            return await ManagedDataStorage.Emoji.QueryAsync<TwEmojiCategory>("GetEmojiCategoriesByName.sql", param);
        }

        public static async Task DeleteById(int id)
        {
            var param = new
            {
                Id = id
            };

            await ManagedDataStorage.Emoji.ExecuteAsync("DeleteEmojiById.sql", param);
        }

        public static async Task<TwEmoji?> GetEmojiByName(string name)
        {
            var param = new
            {
                Name = name
            };

            return await ManagedDataStorage.Emoji.QuerySingleOrDefaultAsync<TwEmoji>("GetEmojiByName.sql", param);
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

            return emojiId;
        }

        public static async Task<List<TwEmoji>> GetAllEmojisPaged(int pageNumber,
            string? orderBy = null, string? orderByDirection = null, List<string>? categories = null)
        {
            var paginationSize = await ConfigurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            if (categories == null || categories.Count == 0)
            {
                var param = new
                {
                    PageNumber = pageNumber,
                    PageSize = paginationSize
                };

                var query = RepositoryHelpers.TransposeOrderby("GetAllEmojisPaged.sql", orderBy, orderByDirection);
                return await ManagedDataStorage.Emoji.QueryAsync<TwEmoji>(query, param);
            }
            else
            {
                var emojiCategoryIds = await SearchEmojiCategoryIds(categories);
                var param = new
                {
                    PageNumber = pageNumber,
                    PageSize = paginationSize
                };

                return await ManagedDataStorage.Emoji.EphemeralAsync(async o =>
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
    }
}
