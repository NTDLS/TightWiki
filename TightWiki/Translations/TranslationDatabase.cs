using Microsoft.Data.Sqlite;
using System.Globalization;
using TightWiki.Library;

namespace TightWiki.Translations
{
    /// <summary>
    /// Holds the translations from Translations.db in memory. The database ships with the application and never changes
    ///  while it is running, and a lookup is made for every piece of text on a page, so each language is loaded from the
    ///  database once (the first time it is needed) and is then served from a dictionary.
    /// The table has one row per English phrase and one column per language.
    /// </summary>
    public class TranslationDatabase
    {
        private const string SourceLanguage = "English";

        private readonly string _databaseFile;
        private readonly bool _isAvailable;
        private readonly SupportedCultures _supportedCultures = new();

        /// <summary>
        /// Translations by language name, then by English phrase (case insensitive).
        /// </summary>
        private readonly Dictionary<string, Lazy<Dictionary<string, string>>> _languages = new(StringComparer.OrdinalIgnoreCase);

        public TranslationDatabase()
            : this(Path.Combine(AppContext.BaseDirectory, "Translations", "Translations.db"))
        {
        }

        public TranslationDatabase(string databaseFile)
        {
            _databaseFile = databaseFile;

            //Opening a missing SQLite file would create an empty database, so we check first.
            _isAvailable = File.Exists(databaseFile);
            if (!_isAvailable)
            {
                //Without translations the application still works, it just displays the English text.
                Console.Error.WriteLine($"The translation database '{databaseFile}' was not found. All text will be displayed in English.");
            }

            foreach (var culture in _supportedCultures.Collection.Where(o => o.Name != SourceLanguage))
            {
                var language = culture.Name;
                _languages[language] = new Lazy<Dictionary<string, string>>(() => Load(language));
            }
        }

        private Dictionary<string, string> Load(string language)
        {
            var phrases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (!_isAvailable)
            {
                return phrases;
            }

            //The language is the name of a column, which cannot be a parameter, but it is always one of the known languages.
            var column = $"\"{language}\"";

            using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = _databaseFile,
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false
            }.ToString());
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT English, {column} FROM Translation WHERE {column} IS NOT NULL AND {column} != ''";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                phrases[reader.GetString(0)] = reader.GetString(1);
            }

            return phrases;
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
