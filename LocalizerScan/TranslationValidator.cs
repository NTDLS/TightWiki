using System.Text.RegularExpressions;

namespace LocalizerScan
{
    /// <summary>
    /// Detects machine translations that are certainly broken, so they can be rejected and translated again.
    /// Only precise checks are used here, since anything flagged is discarded.
    /// </summary>
    internal static partial class TranslationValidator
    {
        private enum Script { Latin, Cyrillic, Greek, Arabic, Hebrew, Devanagari, Bengali, Georgian, Thai, Hangul, Hiragana, Katakana, Han, Other }

        private static readonly (int Start, int End, Script Script)[] _ranges =
        [
            (0x0041, 0x024F, Script.Latin), (0x0250, 0x02AF, Script.Latin), (0x1E00, 0x1EFF, Script.Latin),
            (0x2C60, 0x2C7F, Script.Latin), (0xA720, 0xA7FF, Script.Latin), (0xFF21, 0xFF5A, Script.Latin),
            (0x0370, 0x03FF, Script.Greek), (0x1F00, 0x1FFF, Script.Greek),
            (0x0400, 0x052F, Script.Cyrillic),
            (0x0590, 0x05FF, Script.Hebrew),
            (0x0600, 0x06FF, Script.Arabic), (0x0750, 0x077F, Script.Arabic), (0xFB50, 0xFDFF, Script.Arabic), (0xFE70, 0xFEFF, Script.Arabic),
            (0x0900, 0x097F, Script.Devanagari),
            (0x0980, 0x09FF, Script.Bengali),
            (0x0E00, 0x0E7F, Script.Thai),
            (0x10A0, 0x10FF, Script.Georgian),
            (0x1100, 0x11FF, Script.Hangul), (0x3130, 0x318F, Script.Hangul), (0xAC00, 0xD7AF, Script.Hangul),
            (0x3040, 0x309F, Script.Hiragana),
            (0x30A0, 0x30FF, Script.Katakana), (0xFF66, 0xFF9F, Script.Katakana),
            (0x3400, 0x4DBF, Script.Han), (0x4E00, 0x9FFF, Script.Han), (0xF900, 0xFAFF, Script.Han),
        ];

        /// <summary>
        /// The alphabets each language is written in. Latin is also accepted in every language, for product names and acronyms.
        /// </summary>
        private static readonly Dictionary<string, Script[]> _expectedScripts = new(StringComparer.OrdinalIgnoreCase)
        {
            ["be"] = [Script.Cyrillic],
            ["bg"] = [Script.Cyrillic],
            ["kk"] = [Script.Cyrillic],
            ["ru"] = [Script.Cyrillic],
            ["uk"] = [Script.Cyrillic],
            ["sr"] = [Script.Cyrillic],
            ["ar"] = [Script.Arabic],
            ["fa"] = [Script.Arabic],
            ["ur"] = [Script.Arabic],
            ["he"] = [Script.Hebrew],
            ["el"] = [Script.Greek],
            ["bn"] = [Script.Bengali],
            ["hi"] = [Script.Devanagari],
            ["ka"] = [Script.Georgian],
            ["th"] = [Script.Thai],
            ["ko"] = [Script.Hangul, Script.Han],
            ["ja"] = [Script.Hiragana, Script.Katakana, Script.Han],
            ["zh-Hans"] = [Script.Han],
            ["zh-Hant"] = [Script.Han],
        };

        /// <summary>
        /// Languages written without spaces between words, where Latin terms are commonly joined directly to native words.
        /// </summary>
        private static readonly HashSet<string> _unspacedLanguages = new(StringComparer.OrdinalIgnoreCase) { "ja", "ko", "th", "zh-Hans", "zh-Hant" };

        [GeneratedRegex(@"\{\d+\}")]
        private static partial Regex Placeholder();

        [GeneratedRegex(@"</?([a-zA-Z][a-zA-Z0-9]*)[^>]*>")]
        private static partial Regex Tag();

        [GeneratedRegex(@"\p{L}+")]
        private static partial Regex Word();

        /// <summary>
        /// Returns the reasons that a translation is broken, or an empty list if it looks valid.
        /// </summary>
        public static List<string> GetProblems(string languageCode, string key, string value)
        {
            var problems = new List<string>();

            if (value.Contains('�'))
            {
                problems.Add("contains an invalid character (U+FFFD)");
            }

            if (value.Contains("<Phrase") || value.Contains("</Phrase") || value.Contains("Phrase_"))
            {
                problems.Add("contains leftover phrase tags");
            }

            //Placeholders must match exactly, and there must be no other braces, or string.Format will fail or lose arguments.
            var keyPlaceholders = Placeholder().Matches(key).Select(o => o.Value).Order().ToList();
            var valuePlaceholders = Placeholder().Matches(value).Select(o => o.Value).Order().ToList();
            if (!keyPlaceholders.SequenceEqual(valuePlaceholders)
                || value.Count(c => c == '{') != valuePlaceholders.Count
                || value.Count(c => c == '}') != valuePlaceholders.Count)
            {
                problems.Add($"placeholders differ ({string.Join(" ", keyPlaceholders)} -> {string.Join(" ", valuePlaceholders)})");
            }

            //Markup that the English does not have is shown literally (such as a stray <strong>), unless the English
            //  mentions it by name, for example "the body tag" translated as "<body> 标签".
            var keyTags = Tag().Matches(key).Select(o => o.Groups[1].Value.ToLowerInvariant()).ToHashSet();
            var keyWords = Word().Matches(key).Select(o => o.Value.ToLowerInvariant()).ToHashSet();
            var addedTags = Tag().Matches(value).Select(o => o.Groups[1].Value.ToLowerInvariant())
                .Where(tag => !keyTags.Contains(tag) && !keyWords.Contains(tag)).Distinct().ToList();
            if (addedTags.Count > 0)
            {
                problems.Add($"adds markup ({string.Join(", ", addedTags.Select(t => $"<{t}>"))})");
            }

            if (_expectedScripts.TryGetValue(languageCode, out var expected))
            {
                //Letters from any other alphabet (for example Cyrillic in an Arabic translation) are a sign of a garbled translation.
                var unexpected = value.Where(char.IsLetter).Select(ScriptOf)
                    .Where(s => s != Script.Latin && s != Script.Other && !expected.Contains(s)).Distinct().ToList();
                if (unexpected.Count > 0)
                {
                    problems.Add($"contains {string.Join(", ", unexpected)} letters");
                }
            }
            else
            {
                //Languages written in the Latin alphabet.
                var nonLatin = value.Where(char.IsLetter).Select(ScriptOf).Where(s => s != Script.Latin && s != Script.Other).Distinct().ToList();
                if (nonLatin.Count > 0)
                {
                    problems.Add($"contains {string.Join(", ", nonLatin)} letters");
                }
            }

            //A single word mixing alphabets (such as "Emoсilərə" with a Cyrillic "с") is garbled, except that unspaced
            //  languages join Latin terms directly to native words.
            if (!_unspacedLanguages.Contains(languageCode))
            {
                bool allowsPrefixedAcronyms = _prefixingLanguages.Contains(languageCode);

                var mixed = Word().Matches(value).Select(o => o.Value)
                    .Where(word => !(allowsPrefixedAcronyms && IsPrefixedAcronym(word)))
                    .FirstOrDefault(word => word.Select(ScriptOf).Where(s => s != Script.Other).Distinct().Count() > 1);
                if (mixed != null)
                {
                    problems.Add($"mixes alphabets within the word \"{mixed}\"");
                }
            }

            return problems;
        }

        /// <summary>
        /// Languages that attach one-letter prefixes (such as "and" or "the") directly to the following word, including acronyms.
        /// </summary>
        private static readonly HashSet<string> _prefixingLanguages = new(StringComparer.OrdinalIgnoreCase) { "ar", "fa", "ur", "he" };

        /// <summary>
        /// True for a native prefix followed by an upper-case Latin acronym, such as the Arabic "وCSS" ("and CSS").
        /// </summary>
        private static bool IsPrefixedAcronym(string word)
        {
            int latinStart = 0;
            while (latinStart < word.Length && ScriptOf(word[latinStart]) != Script.Latin)
            {
                latinStart++;
            }

            var acronym = word[latinStart..];
            return latinStart > 0 && latinStart <= 2 && acronym.Length > 0
                && acronym.All(c => ScriptOf(c) == Script.Latin && char.IsUpper(c));
        }

        private static Script ScriptOf(char c)
        {
            foreach (var (start, end, script) in _ranges)
            {
                if (c >= start && c <= end)
                {
                    return script;
                }
            }
            return Script.Other;
        }
    }
}
