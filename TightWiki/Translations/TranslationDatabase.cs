using Microsoft.Data.Sqlite;
using System.Globalization;
using TightWiki.Library;

namespace TightWiki.Translations
{
    /// <summary>
    /// Holds all of the translations from the read-only Translations.db, which is shipped with the application.
    /// The table has one row per English phrase and one column per language, so everything is loaded once at startup
    ///  and looked up in memory (the whole table is small).
    /// </summary>
    public class TranslationDatabase
    {
        private const string FileName = "Translations.db";
        private const string SourceLanguage = "English";

        private readonly SupportedCultures _supportedCultures = new();

        /// <summary>
        /// Translations by language name (the column name), then by English phrase (case insensitive).
        /// </summary>
        private readonly Dictionary<string, Dictionary<string, string>> _translations = new(StringComparer.OrdinalIgnoreCase);

        public TranslationDatabase()
            : this(Path.Combine(AppContext.BaseDirectory, "Translations", FileName))
        {
        }

        public TranslationDatabase(string databaseFile)
        {
            if (!File.Exists(databaseFile))
            {
                //Without translations the application still works, it just displays the English text.
                Console.Error.WriteLine($"The translation database '{databaseFile}' was not found. All text will be displayed in English.");
                return;
            }

            using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = databaseFile,
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false
            }.ToString());
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Translation";

            using var reader = command.ExecuteReader();

            var languageColumns = new List<(int Ordinal, Dictionary<string, string> Phrases, string Language)>();
            for (int ordinal = 1; ordinal < reader.FieldCount; ordinal++) //Column zero is the English phrase.
            {
                var phrases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var language = reader.GetName(ordinal);
                _translations[language] = phrases;
                languageColumns.Add((ordinal, phrases, language));
            }

            while (reader.Read())
            {
                var english = reader.GetString(0);

                foreach (var (ordinal, phrases, _) in languageColumns)
                {
                    if (!reader.IsDBNull(ordinal))
                    {
                        var translation = reader.GetString(ordinal);
                        if (translation.Length > 0)
                        {
                            phrases[english] = translation;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the translation of an English phrase for the given culture. Returns false when there is
        ///  no translation, in which case the caller should fall back to the English phrase.
        /// </summary>
        public bool TryGetTranslation(CultureInfo culture, string english, out string translation)
        {
            translation = string.Empty;

            if (TryGetLanguage(culture, out var language) && language == SourceLanguage)
            {
                translation = english; //The phrases are English, so there is nothing to look up.
                return true;
            }

            if (!string.IsNullOrEmpty(language)
                && _translations.TryGetValue(language, out var phrases)
                && phrases.TryGetValue(english, out var found))
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
