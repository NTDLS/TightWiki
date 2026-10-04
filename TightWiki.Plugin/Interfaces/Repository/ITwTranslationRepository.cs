using NTDLS.SqliteDapperWrapper;
using TightWiki.Plugin.Models;

namespace TightWiki.Plugin.Interfaces.Repository
{
    /// <summary>
    /// Data access for the translations of the user interface text. The translation database is read-only
    /// and ships with the application, it has one row per English phrase and one column per language.
    /// </summary>
    public interface ITwTranslationRepository
    {
        /// <summary>
        /// SQLite factory used to access the translation database.
        /// </summary>
        SqliteManagedFactory TranslationFactory { get; }

        /// <summary>
        /// Returns true if the translation database exists. When it does not, there are no translations and the English text is used.
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Returns every phrase that has been translated to the given language, such as "German" or "Chinese simplified".
        /// Phrases that have not been translated are not returned. The language must be one of the supported languages.
        /// </summary>
        Task<List<TwTranslationPhrase>> GetTranslationsByLanguage(string language);
    }
}
