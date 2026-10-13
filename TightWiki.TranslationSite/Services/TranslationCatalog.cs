using Microsoft.Data.Sqlite;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using TightWiki.Library;

namespace TightWiki.TranslationSite.Services
{
    public sealed record LanguageInfo(string Code, string Name, bool IsRtl);

    /// <summary>
    /// An English phrase and everything it has been translated to.
    /// </summary>
    public sealed class PhraseEntry
    {
        /// <summary>
        /// A short stable identifier for the phrase, derived from the English text.
        /// </summary>
        public required string Key { get; init; }
        public required string English { get; init; }

        /// <summary>
        /// Translations by language name. Languages without a translation are absent.
        /// </summary>
        public required IReadOnlyDictionary<string, string> Translations { get; init; }

        /// <summary>
        /// The same values as above, lower-cased without accents, for searching.
        /// </summary>
        public required IReadOnlyDictionary<string, string> Normalized { get; init; }
        public required string NormalizedEnglish { get; init; }
    }

    public sealed record PhraseMatch(PhraseEntry Phrase, IReadOnlyList<string> MatchedLanguages);

    public sealed record SearchResults(IReadOnlyList<PhraseMatch> Items, int Total);

    public sealed record TranslationGap(PhraseEntry Phrase, LanguageInfo Language);

    /// <summary>
    /// The contents of Translations.db, held in memory. The table is only a few megabytes and never changes
    ///  while the site is running, so every search is done against the in-memory copy.
    /// </summary>
    public sealed class TranslationCatalog
    {
        public const string SourceLanguage = "English";

        private static readonly HashSet<string> _rtlCodes = new(StringComparer.OrdinalIgnoreCase) { "ar", "fa", "he", "ur" };

        private readonly List<PhraseEntry> _phrases = new();
        private readonly Dictionary<string, PhraseEntry> _byKey = new(StringComparer.Ordinal);
        private readonly Dictionary<string, PhraseEntry> _byEnglish = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, LanguageInfo> _languagesByName = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<LanguageInfo> Languages { get; }
        public IReadOnlyList<PhraseEntry> Phrases => _phrases;
        public int PhraseCount => _phrases.Count;

        public TranslationCatalog(string databaseFile)
        {
            var cultures = new SupportedCultures().Collection;

            Languages = cultures
                .Where(o => o.Name != SourceLanguage)
                .Select(o => new LanguageInfo(o.Code, o.Name, _rtlCodes.Contains(o.Code)))
                .OrderBy(o => o.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            foreach (var language in Languages)
            {
                _languagesByName[language.Name] = language;
            }

            if (!File.Exists(databaseFile))
            {
                //Opening a missing SQLite file would create an empty database, so we check first.
                throw new FileNotFoundException("The translation database was not found.", databaseFile);
            }

            Load(databaseFile);
        }

        private void Load(string databaseFile)
        {
            using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = databaseFile,
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false
            }.ToString());
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Translation ORDER BY English COLLATE NOCASE";

            using var reader = command.ExecuteReader();

            var columns = new List<(int Ordinal, string Language)>();
            for (int ordinal = 0; ordinal < reader.FieldCount; ordinal++)
            {
                var name = reader.GetName(ordinal);
                if (_languagesByName.TryGetValue(name, out var language))
                {
                    columns.Add((ordinal, language.Name));
                }
            }

            var englishOrdinal = reader.GetOrdinal("English");

            while (reader.Read())
            {
                var english = reader.GetString(englishOrdinal);

                var translations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var (ordinal, language) in columns)
                {
                    if (!reader.IsDBNull(ordinal))
                    {
                        var value = reader.GetString(ordinal);
                        if (value.Length > 0)
                        {
                            translations[language] = value;
                            normalized[language] = Normalize(value);
                        }
                    }
                }

                var entry = new PhraseEntry
                {
                    Key = CreateKey(english),
                    English = english,
                    Translations = translations,
                    Normalized = normalized,
                    NormalizedEnglish = Normalize(english)
                };

                _phrases.Add(entry);
                _byKey[entry.Key] = entry;
                _byEnglish[english] = entry;
            }
        }

        public PhraseEntry? FindByKey(string key) => _byKey.GetValueOrDefault(key);

        public PhraseEntry? FindByEnglish(string english) => _byEnglish.GetValueOrDefault(english);

        public LanguageInfo? FindLanguage(string? name)
            => string.IsNullOrWhiteSpace(name) ? null : _languagesByName.GetValueOrDefault(name.Trim());

        /// <summary>
        /// Searches the English phrases and the translations. Every word must be found, in any language,
        ///  unless a language is given in which case only English and that language are searched.
        /// An empty query returns everything.
        /// </summary>
        public SearchResults Search(string? query, string? language, int skip, int take)
        {
            var tokens = Normalize(query ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var languageInfo = FindLanguage(language);

            var matches = new List<PhraseMatch>();

            foreach (var phrase in _phrases)
            {
                if (tokens.Length == 0)
                {
                    matches.Add(new PhraseMatch(phrase, Array.Empty<string>()));
                    continue;
                }

                var matchedLanguages = new List<string>();
                bool allTokensFound = true;

                foreach (var token in tokens)
                {
                    bool found = phrase.NormalizedEnglish.Contains(token, StringComparison.Ordinal);

                    if (languageInfo != null)
                    {
                        if (phrase.Normalized.TryGetValue(languageInfo.Name, out var value) && value.Contains(token, StringComparison.Ordinal))
                        {
                            found = true;
                            if (!matchedLanguages.Contains(languageInfo.Name))
                            {
                                matchedLanguages.Add(languageInfo.Name);
                            }
                        }
                    }
                    else
                    {
                        foreach (var (name, value) in phrase.Normalized)
                        {
                            if (value.Contains(token, StringComparison.Ordinal))
                            {
                                found = true;
                                if (!matchedLanguages.Contains(name))
                                {
                                    matchedLanguages.Add(name);
                                }
                            }
                        }
                    }

                    if (!found)
                    {
                        allTokensFound = false;
                        break;
                    }
                }

                if (allTokensFound)
                {
                    matches.Add(new PhraseMatch(phrase, matchedLanguages));
                }
            }

            return new SearchResults(matches.Skip(skip).Take(take).ToList(), matches.Count);
        }

        /// <summary>
        /// Finds phrases that have no translation in some language, so that people can be asked to fill them in.
        /// </summary>
        public List<TranslationGap> GetGaps(int take, Random? random = null)
        {
            random ??= Random.Shared;

            var gaps = new List<TranslationGap>();
            foreach (var phrase in _phrases)
            {
                foreach (var language in Languages)
                {
                    if (!phrase.Translations.ContainsKey(language.Name))
                    {
                        gaps.Add(new TranslationGap(phrase, language));
                    }
                }
            }

            return gaps.OrderBy(_ => random.Next()).Take(take).ToList();
        }

        public int GapCount => _phrases.Sum(p => Languages.Count(l => !p.Translations.ContainsKey(l.Name)));

        public static string CreateKey(string english)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(english.ToLowerInvariant()));
            return Convert.ToHexString(bytes, 0, 6).ToLowerInvariant();
        }

        /// <summary>
        /// Lower-cases and strips accents, so that "cafe" finds "Café".
        /// </summary>
        public static string Normalize(string value)
        {
            var decomposed = value.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(decomposed.Length);

            foreach (var c in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(char.ToLowerInvariant(c));
                }
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
