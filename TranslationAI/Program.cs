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
            //If you process some new languages, update "IsUIComplete" in TightWiki.Library.SupportedCultures.
            var languages = new Dictionary<string, string?>
            {
                { "ar", "Arabic" }, //Done.
                { "bn", "Bengali" }, //Done.
                { "cs", "Czech" }, //Done.
                { "de", "German" }, //Done.
                //{ "en", "English" }, //Done.
                { "es", "Spanish" }, //Done.
                { "fr", "French" }, //Done.
                { "hi", "Hindi" }, //Done.
                { "it", "Italian" }, //Done.
                { "ja", "Japanese" }, //Done.
                { "ko", "Korean" }, //Done.
                { "pt", "Portuguese" }, //Done.
                { "ru", "Russian" }, //Done.
                { "sk", "Slovak" }, //Done.
                { "tr", "Turkish" }, //Done.
                { "uk", "Ukrainian" },
                { "ur", "Urdu" }, //Done.
                { "zh-Hans", "Chinese simplified" }, //Done.
                { "zh-Hant", "Chinese traditional" }, //Done.
                
                //{ "fa", "Persian" },
                //{ "id", "Indonesian" },
                //{ "nl", "Dutch" },
                //{ "pl", "Polish" },
                //{ "vi", "Vietnamese" },

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
                    Console.WriteLine($"{language.Value}: {Path.GetFileName(sourceFileName)}");

                    var targetFileName = sourceFileName.Replace(sourceExt, targetExt);

                    if (File.Exists(targetFileName))
                    {
                        Console.WriteLine($"Skipping {targetFileName} because it already exists.");
                        continue;
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

                    var splitPhrases = translatedBlock.Split('\n');

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
