using NTDLS.SqliteDapperWrapper;
using TightWiki.Plugin.Models;

namespace TightWiki.Plugin.Interfaces.Repository
{
    /// <summary>
    /// Data access for emojis, emoji categories, and related data.
    /// </summary>
    public interface ITwEmojiRepository
    {
        /// <summary>
        /// SQLite factory used to access the emoji database.
        /// </summary>
        SqliteManagedFactory EmojiFactory { get; }

        /// <summary>
        /// Returns all emojis in the database.
        /// </summary>
        Task<List<TwEmoji>> GetAllEmojis();

        /// <summary>
        /// Returns emoji names that match the given search term, for use in autocomplete suggestions.
        /// </summary>
        Task<List<string>> AutoCompleteEmoji(string term);

        /// <summary>
        /// Returns all emojis belonging to the specified category.
        /// </summary>
        Task<List<TwEmoji>> GetEmojisByCategory(string category);

        /// <summary>
        /// Returns all emoji categories grouped for display purposes.
        /// </summary>
        Task<List<TwEmojiCategory>> GetEmojiCategoriesGrouped();

        /// <summary>
        /// Returns the IDs of emoji categories that match any of the specified category names.
        /// </summary>
        Task<List<int>> SearchEmojiCategoryIds(List<string> categories);

        /// <summary>
        /// Returns all emoji categories that match the specified name.
        /// </summary>
        Task<List<TwEmojiCategory>> GetEmojiCategoriesByName(string name);

        /// <summary>
        /// Deletes an emoji by its unique identifier.
        /// </summary>
        Task DeleteById(int id);

        /// <summary>
        /// Returns the emoji with the specified name, or null if not found.
        /// </summary>
        Task<TwEmoji?> GetEmojiByName(string name);

        /// <summary>
        /// Inserts or updates an emoji record. Returns the ID of the affected emoji.
        /// </summary>
        Task<int> UpsertEmoji(TwUpsertEmoji emoji);

        /// <summary>
        /// Returns a paged list of all emojis, with optional sorting and category filtering.
        /// </summary>
        Task<List<TwEmoji>> GetAllEmojisPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null, List<string>? categories = null);

        /// <summary>
        /// Reloads all emojis from the source data. Optionally preloads animated emojis at the specified default height.
        /// </summary>
        Task<List<TwEmoji>> ReloadEmojis(bool preloadAnimatedEmojis, int defaultEmojiHeight);
    }
}