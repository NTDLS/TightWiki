using System.Text;
using TightWiki.Library;
using TightWiki.Models.DataModels;
using TightWiki.Repository;

namespace DummyPageGenerator
{
    internal static class PageGenerator
    {
        private static object _lockObject = new();

        public static void GeneratePages(Guid userId, Random rand, List<string> namespaces, List<string> tags, List<string> fileNames, ref List<string> recentPageNames)
        {
            try
            {
                Console.WriteLine($"{userId} is making changes.");

                var ns = namespaces[rand.Next(namespaces.Count)];

                var pageName = ns + " :: " + string.Join(" ", WordsRepository.GetRandomWords(3));

                lock (_lockObject)
                {
                    recentPageNames.Add(pageName);
                }

                var body = new StringBuilder();

                body.AppendLine($"##title ##Tag(" + string.Join(' ', GetRandomizedList(tags).Take(rand.Next(1, 4))) + ")");
                body.AppendLine($"##toc");

                body.AppendLine($"==Overview");
                int lines = rand.Next(1, 10);
                for (int i = 0; i < 10; i++)
                {
                    var lineWords = WordsRepository.GetRandomWords(rand.Next(10, 20));
                    lock (_lockObject)
                    {
                        lineWords.AddRange(GetRandomizedList(recentPageNames).Take(rand.Next(1, 2)).Select(o => $"[[{o}]]"));
                    }

                    if (rand.Next(100) >= 95)
                    {
                        //Add dead links (missing pages).
                        lineWords.AddRange(WordsRepository.GetRandomWords(rand.Next(1, 2)).Select(o => $"[[{o}]]"));
                    }

                    body.AppendLine(string.Join(' ', GetRandomizedList(lineWords).Select(o => AddWikiMarkup(rand, o))) + "\r\n");
                }

                body.AppendLine("\r\n");

                body.AppendLine($"==Revision Section");
                body.AppendLine($"This is here for the workload generator to easily modify the page.");
                body.AppendLine($"PLACEHOLDER_FOR_REVISION_TEXT_BEGIN\r\nPLACEHOLDER_FOR_REVISION_TEXT_END\r\n");

                var textWithLinks = WordsRepository.GetRandomWords(rand.Next(5, 10));
                lock (_lockObject)
                {
                    textWithLinks.AddRange(GetRandomizedList(recentPageNames).Take(rand.Next(1, 2)).Select(o => $"[[{o}]]"));
                }
                if (rand.Next(100) >= 95)
                {
                    //Add dead links (missing pages).
                    textWithLinks.AddRange(WordsRepository.GetRandomWords(rand.Next(1, 2)).Select(o => $"[[{o}]]"));
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
                    Description = string.Join(' ', WordsRepository.GetRandomWords(rand.Next(3, 5))),
                };
                int newPageId = TightWiki.Engine.WikiHelper.UpsertPage(page);

                if (rand.Next(100) >= 70)
                {
                    var fileName = fileNames[rand.Next(fileNames.Count)] + ".txt"; ;
                    var fileData = Encoding.UTF8.GetBytes(page.Body);
                    AttachFile(newPageId, userId, fileName, fileData);
                }

                lock (_lockObject)
                {
                    recentPageNames = GetRandomizedList(recentPageNames).Take(100).ToList();
                }

                var pagesToModify = PageRepository.GetAllPages().OrderBy(o => rand.Next()).Take(rand.Next(2, 5));

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

                        pageToModify.Body = topText.Trim() + "\r\n" + string.Join(' ', WordsRepository.GetRandomWords(rand.Next(3, 5))) + "\r\n" + bottomText.Trim();
                        pageToModify.ModifiedByUserId = userId;
                        pageToModify.ModifiedByUserId = userId;
                        TightWiki.Engine.WikiHelper.UpsertPage(pageToModify);

                        if (rand.Next(100) >= 90)
                        {
                            var fileName = fileNames[rand.Next(fileNames.Count)] + ".txt";
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
                int k = rand.Next(n + 1);
                T value = newList[k];
                newList[k] = newList[n];
                newList[n] = value;
            }
            return newList;
        }

        static string AddWikiMarkup(Random rand, string text)
        {
            switch (rand.Next(0, 10))
            {
                case 2:
                    return $"//{text}//";
                case 4:
                    return $"~~{text}~~";
                case 6:
                    return $"__{text}__";
                case 8:
                    return $"!!{text}!!";
                default:
                    return text;
            }
        }

    }
}
