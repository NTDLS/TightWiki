using TightWiki.Library;
using TightWiki.Models.DataModels;

namespace TightWiki.Repository
{
    public static partial class EmojiRepository
    {
        public static List<Emoji> GetAllEmojis()
            => ManagedDataStorage.Emoji.Query<Emoji>("GetAllEmojis.sql").ToList();

        public static IEnumerable<Emoji> GetEmojisByCategory(string category)
            => ManagedDataStorage.Emoji.Query<Emoji>("GetEmojisByCategory.sql", new { Category = category });

        public static IEnumerable<EmojiCategory> GetEmojiCategoriesGrouped()
            => ManagedDataStorage.Emoji.Query<EmojiCategory>("GetEmojiCategoriesGrouped.sql");

        public static IEnumerable<int> SearchEmojiCategoryIds(List<string> categories)
        {
            return ManagedDataStorage.Emoji.Ephemeral(o =>
            {
                var param = new
                {
                    SearchTokenCount = categories.Count
                };

                using var tempTable = o.CreateTempTableFrom("TempCategories", categories);
                return o.Query<int>("SearchEmojiCategoryIds.sql", param);
            });
        }

        public static List<EmojiCategory> GetEmojiCategoriesByName(string name)
        {
            var param = new
            {
                Name = name
            };

            return ManagedDataStorage.Emoji.Query<EmojiCategory>("GetEmojiCategoriesByName.sql", param).ToList();
        }

        public static void DeleteById(int id)
        {
            var param = new
            {
                Id = id
            };

            ManagedDataStorage.Emoji.Execute("DeleteEmojiById.sql", param);

            GlobalSettings.ReloadEmojis();
        }

        public static Emoji? GetEmojiByName(string name)
        {
            var param = new
            {
                Name = name
            };

            return ManagedDataStorage.Emoji.QuerySingleOrDefault<Emoji>("GetEmojiByName.sql", param);
        }

        public static int UpsertEmoji(UpsertEmoji emoji)
        {
            int emojiId = ManagedDataStorage.Emoji.Ephemeral(o =>
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
                        emoji.Id = o.ExecuteScalar<int>("InsertEmoji.sql", param);
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
                        o.ExecuteScalar<int>("UpdateEmoji.sql", param);
                    }

                    var upsertEmojiCategoriesParam = new
                    {
                        EmojiId = emoji.Id
                    };

                    using var tempTable = o.CreateTempTableFrom("TempEmojiCategories", emoji.Categories, transaction);
                    o.Execute("UpsertEmojiCategories.sql", upsertEmojiCategoriesParam);

                    transaction.Commit();

                    return (int)emoji.Id;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            });

            GlobalSettings.ReloadEmojis();

            return emojiId;
        }

        public static List<Emoji> GetAllEmojisPaged(int pageNumber, int? pageSize, List<string>? categories)
        {
            pageSize ??= ConfigurationRepository.Get<int>("Customization", "Pagination Size");

            if (categories == null || categories.Count == 0)
            {
                var param = new
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                return ManagedDataStorage.Emoji.Query<Emoji>("GetAllEmojisPaged.sql", param).ToList();
            }
            else
            {
                var emojiCategoryIds = SearchEmojiCategoryIds(categories);

                var param = new
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                return ManagedDataStorage.Emoji.Ephemeral(o =>
                {
                    var getAllEmojisPagedByCategoriesParam = new
                    {
                        SearchTokenCount = emojiCategoryIds.Count(),
                        PageNumber = pageNumber,
                        PageSize = pageSize
                    };

                    using var tempTable = o.CreateTempTableFrom("TempEmojiCategoryIds", emojiCategoryIds);
                    return o.Query<Emoji>("GetAllEmojisPagedByCategories.sql", getAllEmojisPagedByCategoriesParam).ToList();
                });
            }
        }
    }
}
