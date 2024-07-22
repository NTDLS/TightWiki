using System.Text;
using TightWiki.Library;
using TightWiki.Models.DataModels;
using TightWiki.Repository;

namespace DummyPageGenerator
{
    internal static class PageGenerator
    {
        private static object _lockObject = new();
        private static List<Page>? _pagePool;
        private static Random _random = new Random();

        private static List<Page> GetPagePool()
        {
            lock (_lockObject)
            {
                if (_random.Next(0, 100) > 95)
                {
                    _pagePool = PageRepository.GetAllPages();
                }
                return _pagePool ??= PageRepository.GetAllPages();
            }
        }

        public static string GetParagraph(int words)
        {
            using var client = new HttpClient();

            var response = client.GetAsync($"https://textsauce.com/api/Paragraph/English/{words}").Result;
            response.EnsureSuccessStatusCode();

            return response.Content.ReadAsStringAsync().Result;
        }

        public static string GenerateWikiParagraph(List<string> recentPageNames, int wordCount)
        {
            var paragraph = GetParagraph(wordCount);
            var tokens = paragraph.Split(' ');

            int replacementCount = _random.Next(2, 10);

            for (int r = 0; r < replacementCount; r++)
            {
                var token = tokens[_random.Next(0, tokens.Length)];

                switch (_random.Next(0, 7))
                {
                    case 2: //Dead link.
                        paragraph = paragraph.Replace(token, $"[[{token}]]");
                        break;
                    case 4: //Wiki markup.
                        paragraph = paragraph.Replace(token, AddWikiMarkup(token));
                        break;
                    case 6: //Good link.
                        string recentPage;

                        lock (_lockObject)
                        {
                            recentPage = recentPageNames[_random.Next(0, recentPageNames.Count)];
                        }

                        paragraph = paragraph.Replace(token, $"[[{recentPage}]]");
                        break;
                }
            }

            return paragraph;
        }

        public static void GeneratePages(Guid userId, Random rand, List<string> namespaces, List<string> tags, List<string> fileNames, ref List<string> recentPageNames)
        {
            try
            {
                Console.WriteLine($"{userId} is making changes.");

                var ns = namespaces[_random.Next(namespaces.Count)];

                var pageName = ns + " :: " + string.Join(" ", WordsRepository.GetRandomWords(3));

                lock (_lockObject)
                {
                    recentPageNames.Add(pageName);
                }

                var body = new StringBuilder();

                body.AppendLine($"##title ##Tag(" + string.Join(' ', GetRandomizedList(tags).Take(_random.Next(1, 4))) + ")");
                body.AppendLine($"##toc");

                body.AppendLine($"==Overview");
                body.AppendLine(GenerateWikiParagraph(recentPageNames, _random.Next(50, 100)));
                body.AppendLine("\r\n");

                body.AppendLine($"==Revision Section");
                body.AppendLine($"This is here for the workload generator to easily modify the page.");
                body.AppendLine($"PLACEHOLDER_FOR_REVISION_TEXT_BEGIN\r\nPLACEHOLDER_FOR_REVISION_TEXT_END\r\n");

                var textWithLinks = WordsRepository.GetRandomWords(_random.Next(5, 10));
                lock (_lockObject)
                {
                    textWithLinks.AddRange(GetRandomizedList(recentPageNames).Take(_random.Next(1, 2)).Select(o => $"[[{o}]]"));
                }
                if (_random.Next(100) >= 95)
                {
                    //Add dead links (missing pages).
                    textWithLinks.AddRange(WordsRepository.GetRandomWords(_random.Next(1, 2)).Select(o => $"[[{o}]]"));
                }

                body.AppendLine($"==See Also");
                body.AppendLine(string.Join(' ', GetRandomizedList(textWithLinks)));
                body.AppendLine("\r\n");

                body.AppendLine($"==Related");
                body.AppendLine($"##related");
                body.AppendLine("\r\n");

                var page = new Page()
                {
                    Name = pageName,
                    Body = body.ToString(),
                    CreatedByUserId = userId,
                    ModifiedByUserId = userId,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                    Description = string.Join(' ', WordsRepository.GetRandomWords(_random.Next(3, 5))),
                };
                int newPageId = TightWiki.Engine.WikiHelper.UpsertPage(page);

                if (_random.Next(100) >= 70)
                {
                    var fileName = fileNames[_random.Next(fileNames.Count)] + ".txt"; ;
                    var fileData = Encoding.UTF8.GetBytes(page.Body);
                    AttachFile(newPageId, userId, fileName, fileData);
                }

                lock (_lockObject)
                {
                    recentPageNames = GetRandomizedList(recentPageNames).Take(100).ToList();
                }

                var pagesToModify = GetPagePool().OrderBy(o => _random.Next()).Take(_random.Next(2, 5));

                foreach (var pageToModify in pagesToModify)
                {
                    string beginTag = "PLACEHOLDER_FOR_REVISION_TEXT_BEGIN";
                    string endTag = "PLACEHOLDER_FOR_REVISION_TEXT_END";

                    int beginIndex = pageToModify.Body.IndexOf(beginTag);
                    int endIndex = pageToModify.Body.IndexOf(endTag);

                    if (beginIndex > 0 && endIndex > beginIndex)
                    {
                        string topText = pageToModify.Body.Substring(0, beginIndex + beginTag.Length);
                        string bottomText = pageToModify.Body.Substring(endIndex);

                        pageToModify.Body = topText.Trim()
                            + "\r\n" + GenerateWikiParagraph(recentPageNames, _random.Next(10, 20))
                            + "\r\n" + bottomText.Trim();
                        pageToModify.ModifiedByUserId = userId;
                        pageToModify.ModifiedByUserId = userId;
                        TightWiki.Engine.WikiHelper.UpsertPage(pageToModify);

                        if (_random.Next(100) >= 90)
                        {
                            var fileName = fileNames[_random.Next(fileNames.Count)] + ".txt";
                            var fileData = Encoding.UTF8.GetBytes(pageToModify.Body);
                            AttachFile(pageToModify.Id, userId, fileName, fileData);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static void AttachFile(int pageId, Guid userId, string fileName, byte[] fileData)
        {
            PageFileRepository.UpsertPageFile(new PageFileAttachment()
            {
                Data = fileData,
                CreatedDate = DateTime.UtcNow,
                PageId = pageId,
                Name = fileName,
                FileNavigation = Navigation.Clean(fileName),
                Size = fileData.Length,
                ContentType = Utility.GetMimeType(fileName)
            }, userId);
        }

        static List<T> GetRandomizedList<T>(List<T> list)
        {
            var newList = new List<T>(list);
            var rand = new Random();
            int n = newList.Count;
            while (n > 1)
            {
                n--;
                int k = _random.Next(n + 1);
                T value = newList[k];
                newList[k] = newList[n];
                newList[n] = value;
            }
            return newList;
        }

        static string AddWikiMarkup(string text)
        {
            switch (_random.Next(0, 5))
            {
                case 1:
                    return $"//{text}//";
                case 2:
                    return $"~~{text}~~";
                case 3:
                    return $"__{text}__";
                case 4:
                    return $"!!{text}!!";
                default:
                    return text;
            }
        }

    }
}
