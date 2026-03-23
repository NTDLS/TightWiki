using Dapper;
using Microsoft.Data.Sqlite;
using OpenAI;
using OpenAI.Chat;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using TightWiki.Library;

namespace LocalizerScan
{
    internal class Program
    {
        private static readonly Regex[] KeyPatterns =
        [
            new(@"Localizer\[""((?:[^""\\]|\\.)*)""\]", RegexOptions.Compiled),
            new(@"Localizer\.Format\(""((?:[^""\\]|\\.)*)""\s*[,\)]", RegexOptions.Compiled),
            new(@"_localizer\[""((?:[^""\\]|\\.)*)""\]", RegexOptions.Compiled),
            new(@"_localizer\.Format\(""((?:[^""\\]|\\.)*)""\s*[,\)]", RegexOptions.Compiled)
        ];

        private static readonly SupportedCultures _supportedCultures = new SupportedCultures();

        private static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: LocalizerScan <rootPath> <resourcePath>");
                return 1;
            }

            var apiKey = File.ReadAllText("C:\\OpenAPIKey.txt").Trim();
            var openAi = new OpenAIClient(apiKey);
            var chat = openAi.GetChatClient("gpt-4.1-nano");

            var rootPath = args[0];
            var resourcePath = args[1];

            ScanSourceFilesAndAddMissingKeys(rootPath, resourcePath);
            FillInMissingTranslations(resourcePath, chat, "English");

            return 0;
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

                var configKeys = configDb.Query<string>(query);
                foreach(var configKey in configKeys)
                {
                    keysToTranslate.Add(configKey);
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
                list.Add(new CultureInfoSettings("", ""));

                foreach (var culture in list)
                {
                    if(culture.Code == "en")
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

        private static void FillInMissingTranslations(string resourcePath, ChatClient chat, string sourceLanguage)
        {
            var sourceFileNames = Directory.GetFiles(resourcePath, $"*.*.resx", SearchOption.TopDirectoryOnly).ToList();

            foreach (var sourceFileName in sourceFileNames)
            {
                var fileName = Path.GetFileName(sourceFileName);

                var parts = fileName.Split('.');

                if (parts.Length != 3)
                {
                    continue; //We only parse when file name is "NAME.langCode.resx"
                }

                if (_supportedCultures.TryGetByCode(parts[1], out var targetLanguage) == false)
                {
                    continue; //We do not have a language map for this file.
                }

                var doc = XDocument.Load(sourceFileName, LoadOptions.PreserveWhitespace);

                // Find all <data> elements that have a name attribute
                var dataElements = doc.Root?
                    .Elements("data")
                    .Where(d => d.Attribute("name") != null)
                    .ToList();

                if (dataElements == null || dataElements.Count == 0)
                {
                    Console.WriteLine("No <data> elements found.");
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
                    //No phrases to translate.
                    continue;
                }

                Console.WriteLine($"{fileName} : {phrases.Count:n0} elements -> {targetLanguage.Name}");

                while (phrases.Any(o => o.Value == null))
                {
                    bool somethingWentWrong = false;

                    var batch = phrases.Where(o => o.Value == null).Take(10).ToDictionary(o => o.Key, o => o.Value);

                    Console.WriteLine($"Processing batch of {batch.Count:n0} elements -> {targetLanguage.Name}");

                    var promptText = EmbeddedResourceReader.LoadText(@"EmbeddedText\OpenAIPrompt.txt")
                        .Replace("{sourceLanguage}", sourceLanguage)
                        .Replace("{targetLanguage}", targetLanguage.Name);

                    //Create a single input block containing all of the phrases to be translated with numeric tags.
                    var inputPhrases = new StringBuilder();
                    int index = 0;
                    foreach (var phrase in batch)
                    {
                        inputPhrases.AppendLine($"<Phrase_{index}>{phrase.Key}</Phrase_{index}>");
                        index++;
                    }

                    ChatCompletion response = chat.CompleteChat([
                            new SystemChatMessage(promptText),
                            new UserChatMessage(inputPhrases.ToString())
                        ]);

                    var translatedBlock = response.Content[0].Text;

                    var splitPhrases = translatedBlock.Split('\n', StringSplitOptions.TrimEntries);

                    if (splitPhrases.Length != batch.Count)
                    {
                        throw new Exception("The count of translation responses do not match the number of inputs.");
                    }

                    //Parse the translated block and update the dictionary with the translated phrases.
                    index = 0;
                    foreach (var phrase in batch)
                    {
                        var startTag = $"<Phrase_{index}>";
                        var endTag = $"</Phrase_{index}>";
                        var startIndex = translatedBlock.IndexOf(startTag) + startTag.Length;
                        var endIndex = translatedBlock.IndexOf(endTag, startIndex);

                        if (endIndex == -1 || startIndex == -1 || endIndex <= startIndex)
                        {
                            somethingWentWrong = true;
                            Console.WriteLine($"Invalid translation response format. Retrying..");

                            //Clear the translations so that they are retried.
                            foreach (var resetPhrase in batch)
                            {
                                phrases[resetPhrase.Key] = null;
                            }
                            break;
                        }

                        var translatedPhrase = translatedBlock.Substring(startIndex, endIndex - startIndex).Trim();

                        phrases[phrase.Key] = translatedPhrase;
                        batch[phrase.Key] = translatedPhrase;

                        index++;
                    }

                    if (somethingWentWrong)
                    {
                        continue; //Something went wrong during parsing, so we skip updating the XML document and retry the batch.
                    }

                    //Loop back through the dataElements and update the XML document with the translated phrases from the dictionary.
                    foreach (var data in dataElements)
                    {
                        var key = data.Attribute("name")?.Value ?? string.Empty;
                        if (!phrases.ContainsKey(key))
                        {
                            continue; //This key was not in our batch, so we skip it.
                        }

                        if (batch.TryGetValue(key, out string? translation))
                        {
                            var valueElem = data.Element("value");
                            if (valueElem == null) //Create the <value> if its missing.
                            {
                                valueElem = new XElement("value");
                                data.AddFirst(valueElem);
                            }

                            if (translation?.Contains("<Phrase") == true || translation?.Contains("</Phrase") == true)
                            {
                                somethingWentWrong = true;
                                Console.WriteLine($"The translation contains unprocessed tags. Retrying..");

                                //Clear the translations so that they are retried.
                                foreach (var resetPhrase in batch)
                                {
                                    phrases[resetPhrase.Key] = null;
                                }
                                break;
                            }

                            valueElem.Value = translation ?? valueElem.Value;
                        }
                    }

                    if (somethingWentWrong)
                    {
                        continue; //Something went wrong during parsing, so we skip updating the XML document and retry the batch.
                    }

                    doc.Save(sourceFileName);
                }
            }
        }
    }
}
