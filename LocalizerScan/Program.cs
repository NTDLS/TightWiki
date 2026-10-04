using Dapper;
using Microsoft.Data.Sqlite;
using NTDLS.SqliteDapperWrapper;
using System.Text;
using System.Text.RegularExpressions;
using TightWiki.Library;
using VPT.Einkr.Client;

namespace LocalizerScan
{
    internal class Program
    {
        private static readonly Regex[] KeyPatterns =
        [
            new(@"Localize\(""((?:[^""\\]|\\.)*)""\)", RegexOptions.Compiled),
            new(@"Localizer\[""((?:[^""\\]|\\.)*)""\]", RegexOptions.Compiled),
            new(@"Localizer\.Format\(""((?:[^""\\]|\\.)*)""\s*[,\)]", RegexOptions.Compiled),
            new(@"_localizer\[""((?:[^""\\]|\\.)*)""\]", RegexOptions.Compiled),
            new(@"_localizer\.Format\(""((?:[^""\\]|\\.)*)""\s*[,\)]", RegexOptions.Compiled)
        ];

        private static readonly SupportedCultures _supportedCultures = new SupportedCultures();

        private const string ModelName = "Scout-14B";
        private const string EscalationModelName = "Stratum-27B";
        private const int DefaultConcurrency = 4;
        private const int BatchSize = 25;
        private const int MaxBatchAttempts = 5;

        private static readonly Lock _consoleLock = new();
        private static readonly Lock _databaseLock = new(); //SQLite allows only one writer at a time.

        private static async Task<int> Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: LocalizerScan <rootPath> [concurrency]");
                return 1;
            }

            var apiKey = File.ReadAllText("C:\\EinkrKey.txt").Trim();

            //The client is stateless and safe to share, so all of the concurrent translations use one connection pool.
            using var einkr = new EinkrAIClient(ModelName, apiKey);

            var escalationModelName = Environment.GetEnvironmentVariable("LOCALIZER_ESCALATION_MODEL");
            if (string.IsNullOrWhiteSpace(escalationModelName))
            {
                escalationModelName = EscalationModelName;
            }

            using var escalationEinkr = string.IsNullOrWhiteSpace(escalationModelName) ? null : new EinkrAIClient(escalationModelName, apiKey);
            if (escalationEinkr == null)
            {
                Console.WriteLine("No escalation model is configured, failed translations will only be retried with the primary model.");
            }

            var rootPath = args[0];
            var translationDatabase = Path.Join(rootPath, "TightWiki", "Translations", "Translations.db");
            var concurrency = args.Length > 1 && int.TryParse(args[1], out var requested) && requested > 0 ? requested : DefaultConcurrency;

            ScanSourceFilesAndAddMissingKeys(rootPath, translationDatabase);
            ClearInvalidTranslations(translationDatabase);
            await FillInMissingTranslations(translationDatabase, einkr, escalationEinkr, "English", concurrency);

            return 0;
        }

        /// <summary>
        /// Writes a line prefixed with the language it relates to, since several languages are translated at once.
        /// </summary>
        private static void Log(string language, string message)
        {
            lock (_consoleLock)
            {
                Console.WriteLine($"[{language}] {message}");
            }
        }

        private static void ScanSourceFilesAndAddMissingKeys(string rootPath, string translationDatabase)
        {
            try
            {
                Console.WriteLine($"Scanning: {rootPath}");
                if (!Directory.Exists(rootPath))
                {
                    Console.Error.WriteLine("Root path does not exist.");
                    return;
                }

                var sourceCodeFiles = Directory
                    .EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories)
                    .Where(o => o.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                             || o.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase))
                    .Where(o => !o.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                    .Where(o => !o.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var keysToTranslate = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

                using var configDb = new SqliteConnection(@$"Data Source={rootPath}\data\config.db");
                configDb.Open();

                var query = @"SELECT Name FROM ConfigurationEntry WHERE Name != ''
                                UNION SELECT Description FROM ConfigurationEntry WHERE Description != ''
                                UNION SELECT Name FROM ConfigurationGroup WHERE Name != ''
                                UNION SELECT Description FROM ConfigurationGroup WHERE Description != ''";

                //Add configuration groups and entries to the list of keys to translate since these are also user-facing strings that need to be localized.
                var configKeys = configDb.Query<string>(query);
                foreach (var configKey in configKeys)
                {
                    keysToTranslate.Add(configKey);
                }

                //Add the culture names to the list of keys to translate since these are also user-facing strings that need to be localized.
                foreach (var culture in _supportedCultures.Collection)
                {
                    keysToTranslate.Add(culture.Name);
                }

                foreach (var sourceCodeFile in sourceCodeFiles)
                {
                    foreach (var line in File.ReadLines(sourceCodeFile))
                    {
                        foreach (var pattern in KeyPatterns)
                        {
                            foreach (Match match in pattern.Matches(line))
                            {
                                if (!match.Success || match.Groups.Count < 2)
                                    continue;

                                var key = Regex.Unescape(match.Groups[1].Value);

                                if (string.IsNullOrWhiteSpace(key))
                                    continue;

                                keysToTranslate.Add(key);
                            }
                        }
                    }
                }

                Console.WriteLine($"Found {keysToTranslate.Count} unique localization keys.");

                //The English phrase is the primary key (case insensitive), so phrases that are already there are left untouched.
                using var translationDb = new SqliteManagedInstance(translationDatabase);
                using var transaction = translationDb.BeginTransaction();

                int added = 0;
                foreach (var key in keysToTranslate)
                {
                    if(translationDb.ExecuteScalar<int>("INSERT OR IGNORE INTO Translation (English) VALUES (@key)", new { key }) > 0)
                    {
                        Console.WriteLine($"Added \"{key}\" to the translation database.");
                        added++;
                    }
                }

                transaction.Commit();
                Console.WriteLine($"Added {added:n0} new phrases.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
        }

        /// <summary>
        /// Clears any existing translations that fail validation, so that they are translated again.
        /// </summary>
        private static void ClearInvalidTranslations(string translationDatabase)
        {
            int cleared = 0;

            using var translationDb = new SqliteManagedInstance(translationDatabase);

            foreach (var culture in _supportedCultures.Collection.Where(o => o.Code != "en"))
            {
                var column = $"\"{culture.Name}\"";

                var translations = translationDb.Query<(string English, string Value)>(
                    $"SELECT English, {column} as Value FROM Translation WHERE {column} IS NOT NULL AND {column} != ''").ToList();

                using var transaction = translationDb.BeginTransaction();

                foreach (var (key, value) in translations)
                {
                    var problems = TranslationValidator.GetProblems(culture.Code, key, value);
                    if (problems.Count > 0)
                    {
                        Log(culture.Name, $"Cleared \"{key}\" = \"{value}\": {string.Join("; ", problems)}");
                        translationDb.Execute($"UPDATE Translation SET {column} = NULL WHERE English = @key", new { key });
                        cleared++;
                    }
                }

                transaction.Commit();
            }

            Console.WriteLine($"Cleared {cleared:n0} invalid translations.");
        }

        /// <summary>
        /// Translates the languages concurrently. Each language is its own column in the translation table,
        ///  so the only things shared between them are the (thread-safe) clients and the database lock.
        /// </summary>
        private static async Task FillInMissingTranslations(string translationDatabase, EinkrAIClient chat, EinkrAIClient? escalationChat, string sourceLanguage, int concurrency)
        {
            var cultures = _supportedCultures.Collection.Where(o => o.Code != "en").ToList();
            var promptTemplate = EmbeddedResourceReader.LoadText(@"EmbeddedText\SystemPrompt.txt");

            Console.WriteLine($"Translating {cultures.Count:n0} languages, {concurrency} at a time.");

            await Parallel.ForEachAsync(cultures, new ParallelOptions { MaxDegreeOfParallelism = concurrency },
                async (culture, cancellationToken) =>
                {
                    try
                    {
                        await TranslateLanguage(translationDatabase, culture, chat, escalationChat, sourceLanguage, promptTemplate, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        //One failing language should not stop the others.
                        Log(culture.Name, $"Failed: {ex.Message}");
                    }
                });
        }

        private static async Task TranslateLanguage(string translationDatabase, CultureInfoSettings targetLanguage, EinkrAIClient chat, EinkrAIClient? escalationChat,
            string sourceLanguage, string promptTemplate, CancellationToken cancellationToken)
        {
            var languageName = targetLanguage.Name;
            var column = $"\"{languageName}\"";

            //Only the phrases that have no translation need to be translated.
            List<string> missing;
            lock (_databaseLock)
            {
                using var translationDb = new SqliteManagedInstance(translationDatabase);
                missing = translationDb.Query<string>($"SELECT English FROM Translation WHERE {column} IS NULL OR {column} = ''").ToList();
            }

            if (missing.Count == 0)
            {
                return; //No phrases to translate.
            }

            var phrases = missing.ToDictionary(o => o, o => (string?)null);

            Log(languageName, $"{phrases.Count:n0} elements -> {languageName}");

            var promptText = promptTemplate
                .Replace("{sourceLanguage}", sourceLanguage)
                .Replace("{targetLanguage}", languageName);

            //Phrases whose batch keeps failing are left empty, so that the next run tries them again.
            var skipped = new HashSet<string>();

            while (phrases.Any(o => o.Value == null && !skipped.Contains(o.Key)))
            {
                var batch = phrases.Where(o => o.Value == null && !skipped.Contains(o.Key)).Take(BatchSize).Select(o => o.Key).ToList();

                var translations = new Dictionary<string, string>();
                var remaining = batch;

                //Only the phrases that failed validation are retried: first with the primary model, then whatever is
                //  still failing is handed to the larger model. A phrase that one model cannot translate must not
                //  hold up the others, nor keep being retried in ever smaller batches with the same model.
                var models = new List<(EinkrAIClient Client, string Stage)> { (chat, "Processing") };
                if (escalationChat != null)
                {
                    models.Add((escalationChat, "Escalating"));
                }

                foreach (var (client, stage) in models)
                {
                    for (int attempt = 1; attempt <= MaxBatchAttempts && remaining.Count > 0; attempt++)
                    {
                        Log(languageName, $"{stage} batch of {remaining.Count:n0} elements -> {languageName}{(stage == "Escalating" ? " with the larger model" : "")}{(attempt > 1 ? $" (attempt {attempt})" : "")}");

                        var translated = await TranslateBatch(languageName, targetLanguage.Code, client, promptText, remaining, cancellationToken);
                        if (translated == null)
                        {
                            continue; //The response could not be parsed at all.
                        }

                        foreach (var (key, translation) in translated)
                        {
                            translations[key] = translation;
                        }

                        remaining = remaining.Where(o => !translated.ContainsKey(o)).ToList();
                    }
                }

                if (remaining.Count > 0)
                {
                    Log(languageName, $"Giving up on {remaining.Count:n0} elements after {MaxBatchAttempts} attempts{(escalationChat != null ? " with each model" : "")}, they will be retried on the next run.");
                    skipped.UnionWith(remaining);
                }

                if (translations.Count == 0)
                {
                    continue;
                }

                //Save the translated phrases from this batch right away so that they are not lost if a later batch fails.
                lock (_databaseLock)
                {
                    using var translationDb = new SqliteManagedInstance(translationDatabase);
                    using var transaction = translationDb.BeginTransaction();

                    foreach (var (key, translation) in translations)
                    {
                        phrases[key] = translation;
                        translationDb.Execute($"UPDATE Translation SET {column} = @translation WHERE English = @key", new { key, translation });
                    }

                    transaction.Commit();
                }
            }
        }

        /// <summary>
        /// Translates one batch of phrases, returning null if the response could not be parsed. Phrases whose translation
        /// is empty or fails validation are left out of the result (and are retried later), so they do not discard the good ones.
        /// </summary>
        private static async Task<Dictionary<string, string>?> TranslateBatch(string languageName, string languageCode, EinkrAIClient chat, string promptText,
            List<string> batch, CancellationToken cancellationToken)
        {
            //Create a single input block containing all of the phrases to be translated with numeric tags.
            var inputPhrases = new StringBuilder();
            for (int index = 0; index < batch.Count; index++)
            {
                inputPhrases.AppendLine($"<Phrase_{index}>{batch[index]}</Phrase_{index}>");
            }

            ChatCompletion response = await chat.CompleteChatAsync([
                    new SystemChatMessage(promptText),
                    new UserChatMessage(inputPhrases.ToString())
                ], cancellationToken: cancellationToken);

            var translatedBlock = response.Content[0].Text;

            var splitPhrases = translatedBlock.Trim().Split('\n', StringSplitOptions.TrimEntries);
            if (splitPhrases.Length != batch.Count)
            {
                Log(languageName, "The count of translation responses do not match the number of inputs. Retrying..");
                return null;
            }

            //Parse the translated block into the translated phrases.
            var translations = new Dictionary<string, string>();
            for (int index = 0; index < batch.Count; index++)
            {
                var startTag = $"<Phrase_{index}>";
                var endTag = $"</Phrase_{index}>";
                var tagIndex = translatedBlock.IndexOf(startTag);
                var startIndex = tagIndex + startTag.Length;
                var endIndex = tagIndex == -1 ? -1 : translatedBlock.IndexOf(endTag, startIndex);

                if (tagIndex == -1 || endIndex == -1)
                {
                    Log(languageName, "Invalid translation response format. Retrying..");
                    return null;
                }

                var translatedPhrase = translatedBlock.Substring(startIndex, endIndex - startIndex).Trim();

                if (translatedPhrase.Length == 0)
                {
                    Log(languageName, $"The translation of \"{batch[index]}\" is empty. Retrying..");
                    continue;
                }

                var problems = TranslationValidator.GetProblems(languageCode, batch[index], translatedPhrase);
                if (problems.Count > 0)
                {
                    Log(languageName, $"The translation of \"{batch[index]}\" is invalid ({string.Join("; ", problems)}). Retrying..");
                    continue;
                }

                translations[batch[index]] = translatedPhrase;
            }

            return translations;
        }
    }
}
