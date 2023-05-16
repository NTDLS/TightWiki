using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Xml.Linq;
using TightWiki.Shared.ADO;
using TightWiki.Shared.Models.Data;

namespace TightWiki.Shared.Repository
{
    public static partial class EmojiRepository
    {
        public static void UpdatEmojiImage(int emojiId, byte[] imageData)
        {
            using var handler = new SqlConnectionHandler();
            var param = new
            {
                EmojiId = emojiId,
                ImageData = imageData,
            };

            handler.Connection.Execute("UpdatEmojiImage",
                param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
        }

        public static List<EmojiCategory> GetEmojiCategoriesGrouped()
        {
            using var handler = new SqlConnectionHandler();
            return handler.Connection.Query<EmojiCategory>("GetEmojiCategoriesGrouped",
               null, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
        }

        public static List<Emoji> GetEmojisByCategory(string category)
        {
            using var handler = new SqlConnectionHandler();
            return handler.Connection.Query<Emoji>("GetEmojisByCategory",
               new { Category = category }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
        }

        public static List<EmojiCategory> GetEmojiCategoriesByName(string name)
        {
            using var handler = new SqlConnectionHandler();
            return handler.Connection.Query<EmojiCategory>("GetEmojiCategoriesByName",
               new { Name = name }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
        }

        public static Emoji GetEmojiByName(string name)
        {
            using var handler = new SqlConnectionHandler();
            return handler.Connection.Query<Emoji>("GetEmojiByName",
               new { Name = name }, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).FirstOrDefault();
        }

        public static List<Emoji> GetAllEmojis()
        {
            using var handler = new SqlConnectionHandler();
            return handler.Connection.Query<Emoji>("GetAllEmojis",
               null, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
        }

        public static int SaveEmoji(Emoji model)
        {
            using var handler = new SqlConnectionHandler();

            var param = new
            {
                Name = model.Name,
                Categories = model.Categories
            };

            return handler.Connection.ExecuteScalar<int>("SaveEmoji",
               param, null, Singletons.CommandTimeout, CommandType.StoredProcedure);
        }

        public static List<Emoji> GetAllEmojisPaged(int pageNumber, int pageSize, string categories)
        {
            using var handler = new SqlConnectionHandler();
            var param = new
            {
                Categories = categories,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return handler.Connection.Query<Emoji>("GetAllEmojisPaged",
                param, null, true, Singletons.CommandTimeout, CommandType.StoredProcedure).ToList();
        }
    }
}
