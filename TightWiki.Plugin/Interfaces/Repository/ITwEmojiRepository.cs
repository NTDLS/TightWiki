using NTDLS.SqliteDapperWrapper;
using TightWiki.Plugin.Models;

namespace TightWiki.Plugin.Interfaces.Repository
{
    /// <summary>
    ///  Data access for emojis, emoji categories, and related data.
    /// </summary>
    public interface ITwEmojiRepository
    {
        SqliteManagedFactory EmojiFactory { get; }

        Task<List<TwEmoji>> GetAllEmojis();
        Task<List<string>> AutoCompleteEmoji(string term);
        Task<List<TwEmoji>> GetEmojisByCategory(string category);
        Task<List<TwEmojiCategory>> GetEmojiCategoriesGrouped();
        Task<List<int>> SearchEmojiCategoryIds(List<string> categories);
        Task<List<TwEmojiCategory>> GetEmojiCategoriesByName(string name);
        Task DeleteById(int id);
        Task<TwEmoji?> GetEmojiByName(string name);
        Task<int> UpsertEmoji(TwUpsertEmoji emoji);
        Task<List<TwEmoji>> GetAllEmojisPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null, List<string>? categories = null);
        Task<List<TwEmoji>> ReloadEmojis(bool preloadAnimatedEmojis, int defaultEmojiHeight);
    }
}
