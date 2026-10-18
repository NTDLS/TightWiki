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
        private readonly Dictionary<string, LanguageInfo> _languagesByCode = new(StringComparer.OrdinalIgnoreCase);
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
                _languagesByCode[language.Code] = language;
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
            command.CommandText = "SELECT * FROM Translation";

            using var reader = command.ExecuteReader();

            //Each language has a column named by its culture code ("de", "zh-Hans"). Databases from before that rename
            //  used the language name ("German") and called the key column "English", so those are still read.
            var columns = new List<(int Ordinal, string Language)>();
            var phraseOrdinal = -1;
            for (int ordinal = 0; ordinal < reader.FieldCount; ordinal++)
            {
                var name = reader.GetName(ordinal);
                if (name.Equals("Phrase", StringComparison.OrdinalIgnoreCase) || (phraseOrdinal < 0 && name.Equals(SourceLanguage, StringComparison.OrdinalIgnoreCase)))
                {
                    phraseOrdinal = ordinal;
                }
                else if (_languagesByCode.TryGetValue(name, out var byCode) || _languagesByName.TryGetValue(name, out byCode))
                {
                    columns.Add((ordinal, byCode.Name));
                }
            }

            if (phraseOrdinal < 0)
            {
                throw new InvalidDataException("The Translation table has no Phrase column.");
            }

            var rowsByPhrase = new Dictionary<string, object?[]>();
            while (reader.Read())
            {
                var values = new object?[reader.FieldCount];
                reader.GetValues(values);
                rowsByPhrase[reader.GetString(phraseOrdinal)] = values;
            }

            foreach (var (english, values) in rowsByPhrase.OrderBy(o => o.Key, StringComparer.OrdinalIgnoreCase))
            {

                var translations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var (ordinal, language) in columns)
                {
                    if (values[ordinal] is string value)
                    {
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
        /// Finds the language for a language tag such as a browser sends ("de-DE", "pt-BR", "zh-TW", "no"), or null for English
        ///  and for languages that are not supported.
        /// </summary>
        public LanguageInfo? FindLanguageByTag(string? tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return null;
            }

            tag = tag.Trim().Replace('_', '-');

            var exact = Languages.FirstOrDefault(o => o.Code.Equals(tag, StringComparison.OrdinalIgnoreCase));
            if (exact != null)
            {
                return exact;
            }

            var parts = tag.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                return null;
            }

            var primary = parts[0].ToLowerInvariant();

            if (primary == "zh")
            {
                //Browsers send the region (zh-CN, zh-TW), and the two kinds of Chinese are told apart by script.
                var rest = parts.Skip(1).Select(o => o.ToLowerInvariant()).ToList();
                var traditional = rest.Contains("hant") || rest.Contains("tw") || rest.Contains("hk") || rest.Contains("mo");
                return Languages.FirstOrDefault(o => o.Code == (traditional ? "zh-Hant" : "zh-Hans"));
            }

            if (primary == "no")
            {
                primary = "nb"; //Norwegian, which is most often written as Bokmål.
            }

            return Languages.FirstOrDefault(o => o.Code.Equals(primary, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Searches the English phrases and the translations. Every word must be found, in any language,
        ///  unless a language is given in which case only English and that language are searched.
        /// An empty query returns everything.
        /// </summary>
        public SearchResults Search(string? query, string? language, int skip, int take)
        {
            var languageInfo = FindLanguage(language);
            return SearchIn(query, languageInfo == null ? null : [languageInfo.Name], skip, take);
        }

        /// <summary>
        /// The same search, limited to English and a set of languages (all of them when none are given).
        /// </summary>
        public SearchResults SearchIn(string? query, IReadOnlyCollection<string>? languageNames, int skip, int take)
        {
            var tokens = Normalize(query ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var languages = languageNames?
                .Select(FindLanguage)
                .Where(o => o != null)
                .Select(o => o!.Name)
                .Distinct()
                .ToList();

            if (languages is { Count: 0 })
            {
                languages = null;
            }

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

                    foreach (var (name, value) in phrase.Normalized)
                    {
                        if (languages != null && !languages.Contains(name, StringComparer.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (value.Contains(token, StringComparison.Ordinal))
                        {
                            found = true;
                            if (!matchedLanguages.Contains(name))
                            {
                                matchedLanguages.Add(name);
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
            => GetGapsIn(take, null, random);

        /// <summary>
        /// The same, but only for some languages (all of them when none are given).
        /// </summary>
        public List<TranslationGap> GetGapsIn(int take, IReadOnlyCollection<string>? languageNames, Random? random = null)
        {
            random ??= Random.Shared;

            var wanted = languageNames is { Count: > 0 }
                ? Languages.Where(o => languageNames.Contains(o.Name, StringComparer.OrdinalIgnoreCase)).ToList()
                : Languages.ToList();

            var gaps = new List<TranslationGap>();
            foreach (var phrase in _phrases)
            {
                foreach (var language in wanted)
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
