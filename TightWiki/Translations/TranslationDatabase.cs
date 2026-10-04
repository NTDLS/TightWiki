using System.Globalization;
using TightWiki.Library;
using TightWiki.Plugin.Interfaces.Repository;

namespace TightWiki.Translations
{
    /// <summary>
    /// Holds the translations from the translation repository in memory. The translations never change while the
    ///  application is running, and a lookup is made for every piece of text on a page, so each language is loaded
    ///  from the database once (the first time it is needed) and is then served from a dictionary.
    /// </summary>
    public class TranslationDatabase
    {
        private const string SourceLanguage = "English";

        private readonly ITwTranslationRepository _repository;
        private readonly SupportedCultures _supportedCultures = new();

        /// <summary>
        /// Translations by language name, then by English phrase (case insensitive).
        /// </summary>
        private readonly Dictionary<string, Lazy<Dictionary<string, string>>> _languages = new(StringComparer.OrdinalIgnoreCase);

        public TranslationDatabase(ITwTranslationRepository repository)
        {
            _repository = repository;

            foreach (var culture in _supportedCultures.Collection.Where(o => o.Name != SourceLanguage))
            {
                var language = culture.Name;
                _languages[language] = new Lazy<Dictionary<string, string>>(() => Load(language));
            }
        }

        private Dictionary<string, string> Load(string language)
        {
            var phrases = _repository.GetTranslationsByLanguage(language).GetAwaiter().GetResult();
            return phrases.ToDictionary(o => o.English, o => o.Translation, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the translation of an English phrase for the given culture. Returns false when there is
        ///  no translation, in which case the caller should fall back to the English phrase.
        /// </summary>
        public bool TryGetTranslation(CultureInfo culture, string english, out string translation)
        {
            translation = string.Empty;

            if (!TryGetLanguage(culture, out var language))
            {
                return false;
            }

            if (language == SourceLanguage)
            {
                translation = english; //The phrases are English, so there is nothing to look up.
                return true;
            }

            if (_languages.TryGetValue(language, out var phrases) && phrases.Value.TryGetValue(english, out var found))
            {
                translation = found;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Finds the language (column) for the culture, falling back from a regional culture such as "zh-Hans-CN" to its parents.
        /// </summary>
        private bool TryGetLanguage(CultureInfo culture, out string language)
        {
            for (var current = culture; !string.IsNullOrEmpty(current.Name); current = current.Parent)
            {
                if (_supportedCultures.TryGetByCode(current.Name, out var supported))
                {
                    language = supported.Name;
                    return true;
                }
            }

            language = string.Empty;
            return false;
        }
    }
}
