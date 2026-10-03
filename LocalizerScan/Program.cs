using Dapper;
using Microsoft.Data.Sqlite;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
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

        /// <summary>
        /// The larger model that is tried when <see cref="ModelName"/> cannot produce a valid translation for a batch.
        /// Can be overridden with the LOCALIZER_ESCALATION_MODEL environment variable. When empty, there is no escalation.
        /// </summary>
        private const string EscalationModelName = "Stratum-27B";
        private const int DefaultConcurrency = 4;
        private const int BatchSize = 25;
        private const int MaxBatchAttempts = 5;

        private static readonly Lock _consoleLock = new();

        private static async Task<int> Main(string[] args)
        {
            if (args.Length == 2 && args[0].Equals("--validate", StringComparison.OrdinalIgnoreCase))
            {
                //Only checks the existing translations, clearing any that are broken so that the next run retranslates them.
                ClearInvalidTranslations(args[1]);
                return 0;
            }

            if (args.Length < 2)
            {
                Console.WriteLine("Usage: LocalizerScan <rootPath> <resourcePath> [concurrency]");
                Console.WriteLine("       LocalizerScan --validate <resourcePath>");
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
            var resourcePath = args[1];
            var concurrency = args.Length > 2 && int.TryParse(args[2], out var requested) && requested > 0 ? requested : DefaultConcurrency;

            ScanSourceFilesAndAddMissingKeys(rootPath, resourcePath);
            ClearInvalidTranslations(resourcePath);
            await FillInMissingTranslations(resourcePath, einkr, escalationEinkr, "English", concurrency);

            return 0;
        }

        /// <summary>
        /// Writes a line prefixed with the file it relates to, since several files are translated at once.
        /// </summary>
        private static void Log(string fileName, string message)
        {
            lock (_consoleLock)
            {
                Console.WriteLine($"[{fileName}] {message}");
            }
        }

        private static void ScanSourceFilesAndAddMissingKeys(string rootPath, string resourcePath)
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

                var templateXml = EmbeddedResourceReader.LoadText(@"EmbeddedText\TemplateResourceXml.txt");

                Console.WriteLine($"Found {keysToTranslate.Count} unique localization keys.");

                var list = _supportedCultures.Collection.ToList();
                list.Add(new CultureInfoSettings("", "")); //Neutral culture does not have a culture code in the file name.

                foreach (var culture in list)
                {
                    if (culture.Code == "en")
                    {
                        //We skip English because the keys themselves are the English phrases, so there is no need to add them to the resource file.
                        continue;
                    }

                    var resourceFileName = Path.Combine(resourcePath, $"SharedLocalizer.{culture.Code}.resx");

                    if (string.IsNullOrEmpty(culture.Code))
                    {
                        //Neutral culture does not have a culture code in the file name.
                        resourceFileName = Path.Combine(resourcePath, $"SharedLocalizer.resx");
                    }

                    if (File.Exists(resourceFileName) == false)
                    {
                        File.WriteAllText(resourceFileName, templateXml);
                    }

                    var doc = XDocument.Load(resourceFileName);

                    var existingKeys = doc.Root!
                        .Elements("data")
                        .Select(e => e.Attribute("name")?.Value)
                        .Where(v => v != null)
                        .ToHashSet();

                    int added = 0;

                    foreach (var keyMapping in keysToTranslate)
                    {
                        if (!existingKeys.Contains(keyMapping))
                        {
                            Console.WriteLine($"Added {keyMapping} to {culture.Code} resource.");

                            if (string.IsNullOrEmpty(culture.Code))
                            {
                                //Neutral culture needs to have a value, these are the English phrases that we will
                                //  translate from, so we set the value to the key which is the English phrase.
                                doc.Root!.Add(
                                    new XElement("data",
                                        new XAttribute("name", keyMapping),
                                        new XAttribute(XNamespace.Xml + "space", "preserve"),
                                        new XElement("value", keyMapping)
                                    )
                                );
                            }
                            else
                            {
                                doc.Root!.Add(
                                    new XElement("data",
                                        new XAttribute("name", keyMapping),
                                        new XAttribute(XNamespace.Xml + "space", "preserve"),
                                        new XElement("value", string.Empty)
                                    )
                                );
                            }
                            added++;
                        }
                    }

                    doc.Save(resourceFileName);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
        }

        /// <summary>
        /// Clears any existing translations that fail validation, so that they are translated again.
        /// </summary>
        private static void ClearInvalidTranslations(string resourcePath)
        {
            int cleared = 0;

            foreach (var sourceFileName in Directory.GetFiles(resourcePath, "*.*.resx", SearchOption.TopDirectoryOnly))
            {
                var fileName = Path.GetFileName(sourceFileName);
                var parts = fileName.Split('.');
                if (parts.Length != 3)
                {
                    continue; //We only check files named "NAME.langCode.resx"
                }

                var doc = XDocument.Load(sourceFileName, LoadOptions.PreserveWhitespace);
                bool changed = false;

                foreach (var data in doc.Root?.Elements("data") ?? [])
                {
                    var key = data.Attribute("name")?.Value;
                    var valueElem = data.Element("value");
                    if (key == null || string.IsNullOrEmpty(valueElem?.Value))
                    {
                        continue;
                    }

                    var problems = TranslationValidator.GetProblems(parts[1], key, valueElem.Value);
                    if (problems.Count > 0)
                    {
                        Log(fileName, $"Cleared \"{key}\" = \"{valueElem.Value}\": {string.Join("; ", problems)}");
                        valueElem.Value = string.Empty;
                        changed = true;
                        cleared++;
                    }
                }

                if (changed)
                {
                    doc.Save(sourceFileName);
                }
            }

            Console.WriteLine($"Cleared {cleared:n0} invalid translations.");
        }

        /// <summary>
        /// Translates the language files concurrently. Each file has its own XML document and phrase list,
        ///  so the only thing shared between them is the (thread-safe) client.
        /// </summary>
        private static async Task FillInMissingTranslations(string resourcePath, EinkrAIClient chat, EinkrAIClient? escalationChat, string sourceLanguage, int concurrency)
        {
            var sourceFileNames = Directory.GetFiles(resourcePath, $"*.*.resx", SearchOption.TopDirectoryOnly).ToList();
            var promptTemplate = EmbeddedResourceReader.LoadText(@"EmbeddedText\SystemPrompt.txt");

            Console.WriteLine($"Translating {sourceFileNames.Count:n0} files, {concurrency} at a time.");

            await Parallel.ForEachAsync(sourceFileNames, new ParallelOptions { MaxDegreeOfParallelism = concurrency },
                async (sourceFileName, cancellationToken) =>
                {
                    try
                    {
                        await TranslateFile(sourceFileName, chat, escalationChat, sourceLanguage, promptTemplate, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        //One failing language should not stop the others.
                        Log(Path.GetFileName(sourceFileName), $"Failed: {ex.Message}");
                    }
                });
        }

        private static async Task TranslateFile(string sourceFileName, EinkrAIClient chat, EinkrAIClient? escalationChat, string sourceLanguage,
            string promptTemplate, CancellationToken cancellationToken)
        {
            var fileName = Path.GetFileName(sourceFileName);

            var parts = fileName.Split('.');

            if (parts.Length != 3)
            {
                return; //We only parse when file name is "NAME.langCode.resx"
            }

            if (_supportedCultures.TryGetByCode(parts[1], out var targetLanguage) == false)
            {
                return; //We do not have a language map for this file.
            }

            var doc = XDocument.Load(sourceFileName, LoadOptions.PreserveWhitespace);

            // Find all <data> elements that have a name attribute
            var dataElements = doc.Root?
                .Elements("data")
                .Where(d => d.Attribute("name") != null)
                .ToList();

            if (dataElements == null || dataElements.Count == 0)
            {
                Log(fileName, "No <data> elements found.");
                return;
            }

            var phrases = new Dictionary<string, string?>();

            //Build a dictionary containing all of the keys, which are the English phrases.
            foreach (var data in dataElements)
            {
                string key = data.Attribute("name")?.Value ?? "";
                var valueElem = data.Element("value");

                if (string.IsNullOrEmpty(valueElem?.Value) == false)
                {
                    continue; //We only want to translate phrases that have no value.
                }

                if (valueElem == null) //Create the <value> if its missing.
                {
                    valueElem = new XElement("value");
                    data.AddFirst(valueElem);
                }

                phrases.Add(key, null);
            }

            if (phrases.Count == 0)
            {
                return; //No phrases to translate.
            }

            Log(fileName, $"{phrases.Count:n0} elements -> {targetLanguage.Name}");

            var promptText = promptTemplate
                .Replace("{sourceLanguage}", sourceLanguage)
                .Replace("{targetLanguage}", targetLanguage.Name);

            //Phrases whose batch keeps failing are left empty, so that the next run tries them again.
            var skipped = new HashSet<string>();

            while (phrases.Any(o => o.Value == null && !skipped.Contains(o.Key)))
            {
                var batch = phrases.Where(o => o.Value == null && !skipped.Contains(o.Key)).Take(BatchSize).Select(o => o.Key).ToList();

                Dictionary<string, string>? translations = null;
                for (int attempt = 1; attempt <= MaxBatchAttempts && (translations == null || translations.Count == 0); attempt++)
                {
                    Log(fileName, $"Processing batch of {batch.Count:n0} elements -> {targetLanguage.Name}{(attempt > 1 ? $" (attempt {attempt})" : "")}");
                    translations = await TranslateBatch(fileName, parts[1], chat, promptText, batch, cancellationToken);
                }

                //The primary model could not produce anything valid, so hand the batch to the larger model.
                if ((translations == null || translations.Count == 0) && escalationChat != null)
                {
                    for (int attempt = 1; attempt <= MaxBatchAttempts && (translations == null || translations.Count == 0); attempt++)
                    {
                        Log(fileName, $"Escalating batch of {batch.Count:n0} elements -> {targetLanguage.Name} to the larger model{(attempt > 1 ? $" (attempt {attempt})" : "")}");
                        translations = await TranslateBatch(fileName, parts[1], escalationChat, promptText, batch, cancellationToken);
                    }
                }

                if (translations == null || translations.Count == 0)
                {
                    Log(fileName, $"Giving up on a batch of {batch.Count:n0} elements after {MaxBatchAttempts} attempts, they will be retried on the next run.");
                    skipped.UnionWith(batch);
                    continue;
                }

                //Update the XML document with the translated phrases from this batch.
                foreach (var data in dataElements)
                {
                    var key = data.Attribute("name")?.Value ?? string.Empty;
                    if (translations.TryGetValue(key, out var translation))
                    {
                        phrases[key] = translation;

                        var valueElem = data.Element("value");
                        if (valueElem == null) //Create the <value> if its missing.
                        {
                            valueElem = new XElement("value");
                            data.AddFirst(valueElem);
                        }
                        valueElem.Value = translation;
                    }
                }

                doc.Save(sourceFileName);
            }
        }

        /// <summary>
        /// Translates one batch of phrases, returning null if the response could not be parsed. Phrases whose translation
        /// is empty or fails validation are left out of the result (and are retried later), so they do not discard the good ones.
        /// </summary>
        private static async Task<Dictionary<string, string>?> TranslateBatch(string fileName, string languageCode, EinkrAIClient chat, string promptText,
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
                Log(fileName, "The count of translation responses do not match the number of inputs. Retrying..");
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
                    Log(fileName, "Invalid translation response format. Retrying..");
                    return null;
                }

                var translatedPhrase = translatedBlock.Substring(startIndex, endIndex - startIndex).Trim();

                if (translatedPhrase.Length == 0)
                {
                    Log(fileName, $"The translation of \"{batch[index]}\" is empty. Retrying..");
                    continue;
                }

                var problems = TranslationValidator.GetProblems(languageCode, batch[index], translatedPhrase);
                if (problems.Count > 0)
                {
                    Log(fileName, $"The translation of \"{batch[index]}\" is invalid ({string.Join("; ", problems)}). Retrying..");
                    continue;
                }

                translations[batch[index]] = translatedPhrase;
            }

            return translations;
        }
    }
}
