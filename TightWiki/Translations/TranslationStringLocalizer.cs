using Microsoft.Extensions.Localization;
using System.Globalization;

namespace TightWiki.Translations
{
    /// <summary>
    /// Looks up the English phrase in the translation database using the language of the current UI culture.
    /// When there is no translation, the English phrase itself is used.
    /// </summary>
    public class TranslationStringLocalizer
        : IStringLocalizer
    {
        private readonly TranslationDatabase _database;

        public TranslationStringLocalizer(TranslationDatabase database)
        {
            _database = database;
        }

        public LocalizedString this[string name]
        {
            get
            {
                var found = _database.TryGetTranslation(CultureInfo.CurrentUICulture, name, out var translation);
                return new LocalizedString(name, found ? translation : name, resourceNotFound: !found);
            }
        }

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                var found = _database.TryGetTranslation(CultureInfo.CurrentUICulture, name, out var translation);
                return new LocalizedString(name, string.Format(found ? translation : name, arguments), resourceNotFound: !found);
            }
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => Enumerable.Empty<LocalizedString>();
    }

    /// <summary>
    /// All of the shared strings live in one database, so every type and resource name gets the same localizer.
    /// </summary>
    public class TranslationStringLocalizerFactory
        : IStringLocalizerFactory
    {
        private readonly TranslationStringLocalizer _localizer;

        public TranslationStringLocalizerFactory(TranslationDatabase database)
        {
            _localizer = new TranslationStringLocalizer(database);
        }

        public IStringLocalizer Create(Type resourceSource) => _localizer;

        public IStringLocalizer Create(string baseName, string location) => _localizer;
    }
}
