using OpenAI;
using OpenAI.Chat;
using System;
using System.Text;
using System.Xml.Linq;

namespace TranslationAI
{
    internal class Program
    {
        static void Main()
        {
            //If you process some new languages, update "IsUIComplete" in TightWiki.Library.SupportedCultures.
            //Also be sure to add the new keys contining the new language names to the Localization.cs.resx,
            //  this is because "*.cs.resx" is where we source translations from and we need the language names
            //  translated into other languages
            var languages = new Dictionary<string, string>
            {
                //{ "cs", "Czech" }, //Never needs to be done, this was done manually.
                //{ "sk", "Slovak" }, //Never needs to be done, this was done manually.
                //{ "en", "English" }, //Never needs to be done, this is the source.

                { "ar", "Arabic" }, //Done.
                { "bn", "Bengali" }, //Done.
                { "de", "German" }, //Done.
                { "es", "Spanish" }, //Done.
                { "fr", "French" }, //Done.
                { "hi", "Hindi" }, //Done.
                { "it", "Italian" }, //Done.
                { "ja", "Japanese" }, //Done.
                { "ko", "Korean" }, //Done.
                { "pt", "Portuguese" }, //Done.
                { "ru", "Russian" }, //Done.
                { "tr", "Turkish" }, //Done.
                { "uk", "Ukrainian" },
                { "ur", "Urdu" }, //Done.
                { "zh-Hans", "Chinese simplified" }, //Done.
                { "zh-Hant", "Chinese traditional" }, //Done.
                
                //Up next:
                //{ "fa", "Persian" },
                //{ "id", "Indonesian" },
                //{ "nl", "Dutch" },
                //{ "pl", "Polish" },
                //{ "vi", "Vietnamese" },

                //Future if requested:
                //{ "az", "Azerbaijani" },
                //{ "be", "Belarusian" },
                //{ "bg", "Bulgarian" },
                //{ "da", "Danish" },
                //{ "el", "Greek" },
                //{ "et", "Estonian" },
                //{ "fi", "Finnish" },
                //{ "he", "Hebrew" },
                //{ "hr", "Croatian" },
                //{ "hu", "Hungarian" },
                //{ "is", "Icelnadic" },
                //{ "ka", "Georgian" },
                //{ "kk", "Kazakh" },
                //{ "lt", "Lithuianian" },
                //{ "lv", "Latvian" },
                //{ "ms", "Malay" },
                //{ "nn", "Norwegian" },
                //{ "ro", "Romanian" },
                //{ "sl", "Slovenian" },
                //{ "sr", "Serbian" },
                //{ "sv", "Swedish" },
                //{ "th", "Thai" },
            };

            var apiKey = File.ReadAllText("C:\\OpenAPIKey.txt").Trim();
            var openAi = new OpenAIClient(apiKey);
            var chat = openAi.GetChatClient("gpt-4o-mini");

            PerformTranslations(chat, languages, false);

            languages.Add("cs", "Czech");
            languages.Add("sk", "Slovak");

            //If we added any new languages, we need to overwrite the existing files.
            PerformTranslations(chat, languages, true, ["Localization."]);
        }

        private static void PerformTranslations(ChatClient chat, Dictionary<string, string> languages, bool overwriteExisting, List<string>? fileNameFilter = null)
        {
            foreach (var language in languages)
            {
                var sourceExt = "cs.resx";
                var sourceLanguage = "English";
                var targetExt = $"{language.Key}.resx";
                var targetLanguage = language.Value;

                var sourceFileNames = Directory.GetFiles(@"C:\NTDLS\TightWiki\TightWiki\Resources", $"*.{sourceExt}", SearchOption.AllDirectories)
                    .OrderBy(o => o).ToList();

                string prompt = $"You are a translator that translates {sourceLanguage} to {targetLanguage}.\r\nThe text to translate is inside <Phrase_X> tags.\r\nKeep the tags exactly as they are in the output.\r\nIf the text contains placeholders such as {0}, {1}, {2}, etc., retain them exactly in the translation and place them in a position that is natural for {targetLanguage} grammar.\r\nDo not alter, remove, or renumber placeholders.\r\nDo not add any commentary — only return the translated text with the original tags and placeholders intact.";

                foreach (var sourceFileName in sourceFileNames)
                {
                    var fileName = Path.GetFileName(sourceFileName);

                    if (fileNameFilter != null)
                    {
                        if (!fileNameFilter.Any(filter => fileName.Contains(filter, StringComparison.OrdinalIgnoreCase)))
                        {
                            Console.WriteLine($"Skipping {fileName} because it does not match the filter.");
                            continue;
                        }
                    }

                    var targetFileName = sourceFileName.Replace(sourceExt, targetExt);
                    if (!overwriteExisting && File.Exists(targetFileName))
                    {
                        Console.WriteLine($"Skipping {targetFileName} because it already exists.");
                        continue;
                    }

                    Console.WriteLine($"{language.Value}: {fileName}");

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
                        if (valueElem == null) //Create the <value> if its missing.
                        {
                            valueElem = new XElement("value");
                            data.AddFirst(valueElem);
                        }

                        phrases.Add(key, null);
                    }

                    //Create a single input block containing all of the phrases to be translated with numeric tags.
                    var inputPhrases = new StringBuilder();
                    int index = 0;
                    foreach (var phrase in phrases)
                    {
                        inputPhrases.AppendLine($"<Phrase_{index}>{phrase.Key}</Phrase_{index}>");
                        index++;
                    }

                    ChatCompletion response = chat.CompleteChat([
                        new SystemChatMessage(prompt),
                        new UserChatMessage(inputPhrases.ToString())
                    ]);

                    var translatedBlock = response.Content[0].Text;

                    var splitPhrases = translatedBlock.Split('\n', StringSplitOptions.TrimEntries);

                    if (splitPhrases.Length != phrases.Count)
                    {
                        throw new Exception("The count of translation responses do not match the number of inputs.");
                    }

                    //Parse the translated block and update the dictionary with the translated phrases.
                    index = 0;
                    foreach (var phrase in phrases)
                    {
                        var startTag = $"<Phrase_{index}>";
                        var endTag = $"</Phrase_{index}>";
                        var startIndex = translatedBlock.IndexOf(startTag) + startTag.Length;
                        var endIndex = translatedBlock.IndexOf(endTag, startIndex);
                        var translatedPhrase = translatedBlock.Substring(startIndex, endIndex - startIndex).Trim();

                        phrases[phrase.Key] = translatedPhrase;

                        index++;
                    }

                    //Update the XML document with the translated phrases.
                    foreach (var data in dataElements)
                    {
                        string key = data.Attribute("name")?.Value ?? "";
                        var valueElem = data.Element("value");
                        if (valueElem == null) //Create the <value> if its missing.
                        {
                            valueElem = new XElement("value");
                            data.AddFirst(valueElem);
                        }

                        valueElem.Value = phrases[key] ?? valueElem.Value;
                    }

                    doc.Save(targetFileName);
                }
            }
        }
    }
}

#region Snippets.

/*
var openAi = new OpenAIClient(apiKey);
ChatClient chat = openAi.GetChatClient("gpt-4o-mini");
ChatCompletion completion = chat.CompleteChat("Say 'this is a test.'");
Console.WriteLine(completion.Content[0].Text);

var openAi = new OpenAIClient(apiKey);
var chat = openAi.GetChatClient("gpt-4o-mini");
ChatCompletion response = chat.CompleteChat([
    new SystemChatMessage("You are a translator that translates English to Spanish."),
    new UserChatMessage("Where is the train station?")
]);
Console.WriteLine(response.Content[0].Text);

ChatCompletion response = chat.CompleteChat([
    new SystemChatMessage("You are a translator that translates English to Spanish."),
    new UserChatMessage("Where is the train station?")
]);

Console.WriteLine(response.Content[0].Text);
*/

#endregion
