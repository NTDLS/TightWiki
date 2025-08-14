using OpenAI;
using OpenAI.Chat;
using System.Text;
using System.Xml.Linq;

namespace TranslationAI
{
    internal class Program
    {
        static void Main()
        {
            //If you add new languages, update "IsUIComplete" in TightWiki.Library.SupportedCultures.
            var languages = new Dictionary<string, string>
            {
                { "cs", "Czech" },
                { "sk", "Slovak" },
                { "en", "English" },
                { "ar", "Arabic" },
                { "bn", "Bengali" },
                { "de", "German" },
                { "es", "Spanish" },
                { "fr", "French" },
                { "hi", "Hindi" },
                { "it", "Italian" },
                { "ja", "Japanese" },
                { "ko", "Korean" },
                { "pt", "Portuguese" },
                { "ru", "Russian" },
                { "tr", "Turkish" },
                { "uk", "Ukrainian" },
                { "ur", "Urdu" },
                { "zh-Hans", "Chinese simplified" },
                { "zh-Hant", "Chinese traditional" },
                { "fa", "Persian" },
                { "id", "Indonesian" },
                { "nl", "Dutch" },
                { "pl", "Polish" },
                { "vi", "Vietnamese" },
                { "az", "Azerbaijani" },
                { "be", "Belarusian" },
                { "bg", "Bulgarian" },
                { "da", "Danish" },
                { "el", "Greek" },
                { "et", "Estonian" },
                { "fi", "Finnish" },
                { "he", "Hebrew" },
                { "hr", "Croatian" },
                { "hu", "Hungarian" },
                { "is", "Icelnadic" },
                { "ka", "Georgian" },
                { "kk", "Kazakh" },
                { "lt", "Lithuianian" },
                { "lv", "Latvian" },
                { "ms", "Malay" },
                { "nn", "Norwegian" },
                { "ro", "Romanian" },
                { "sl", "Slovenian" },
                { "sr", "Serbian" },
                { "sv", "Swedish" },
                { "th", "Thai" },
            };

            var apiKey = File.ReadAllText("C:\\OpenAPIKey.txt").Trim();
            var openAi = new OpenAIClient(apiKey);
            var chat = openAi.GetChatClient("gpt-4o-mini");

            FillInMissingTranslations(chat, "English", languages);
        }

        private static void FillInMissingTranslations(ChatClient chat, string sourceLanguage, Dictionary<string, string> languages)
        {
            var sourceFileNames = Directory.GetFiles(@"C:\NTDLS\TightWiki\TightWiki\Resources", $"*.*.resx", SearchOption.AllDirectories).ToList();

            foreach (var sourceFileName in sourceFileNames)
            {
                var fileName = Path.GetFileName(sourceFileName);

                var parts = fileName.Split('.');

                if (parts.Length != 3)
                {
                    continue; //We only parse when file name is "NAME.langCode.resx"
                }

                if (languages.TryGetValue(parts[1], out string? targetLanguage) == false)
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

                Console.WriteLine($"{fileName} : {phrases.Count:n0} elements -> {targetLanguage}");

                //Create a single input block containing all of the phrases to be translated with numeric tags.
                var inputPhrases = new StringBuilder();
                int index = 0;
                foreach (var phrase in phrases)
                {
                    inputPhrases.AppendLine($"<Phrase_{index}>{phrase.Key}</Phrase_{index}>");
                    index++;
                }

                var prompt = $"You are a translator that translates {sourceLanguage} to {targetLanguage}.\r\nThe text to translate is inside <Phrase_X> tags.\r\nKeep the tags exactly as they are in the output.\r\nIf the text contains placeholders such as {0}, {1}, {2}, etc., retain them exactly in the translation and place them in a position that is natural for {targetLanguage} grammar.\r\nDo not alter, remove, or renumber placeholders.\r\nDo not add any commentary — only return the translated text with the original tags and placeholders intact.";
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

                //Loop back through the dataElements and update the XML document with the translated phrases from the dictionary.
                foreach (var data in dataElements)
                {
                    var key = data.Attribute("name")?.Value ?? string.Empty;

                    if (phrases.TryGetValue(key, out string? translation))
                    {
                        var valueElem = data.Element("value");
                        if (valueElem == null) //Create the <value> if its missing.
                        {
                            valueElem = new XElement("value");
                            data.AddFirst(valueElem);
                        }

                        if (translation?.Contains("<Phrase") == true || translation?.Contains("</Phrase") == true)
                        {
                            throw new Exception($"The translation for key '{key}' contains unprocessed tags. This should not happen. Please check the translation logic.");
                        }

                        valueElem.Value = translation ?? valueElem.Value;
                    }
                }

                doc.Save(sourceFileName);
            }
        }
    }
}
