using System.Text;
using TightWiki.Controllers;
using TightWiki.Models.DataModels;
using TightWiki.Repository;

namespace TightWiki.Dummy
{
    internal static class PageGenerator
    {
        public static void GeneratePages(WikiControllerBase controller, Guid userId)
        {
            var rand = new Random();

            var namespaces = WordsRepository.GetRandomWords(250);
            var tags = WordsRepository.GetRandomWords(250);

            var recentPageNames = new List<string>();

            while (true)
            {
                var ns = namespaces[rand.Next(namespaces.Count)];

                var pageName = ns + " :: " + string.Join(" ", WordsRepository.GetRandomWords(3));

                recentPageNames.Add(pageName);

                var body = new StringBuilder();

                body.AppendLine($"##title ##Tag(" + string.Join(' ', GetRandomizedList(tags).Take(rand.Next(1, 4))) + ")");
                body.AppendLine($"##toc");

                body.AppendLine($"==Overview");
                int lines = rand.Next(1, 10);
                for (int i = 0; i < 10; i++)
                {
                    var lineWords = WordsRepository.GetRandomWords(rand.Next(10, 20));
                    lineWords.AddRange(GetRandomizedList(recentPageNames).Take(rand.Next(1, 2)).Select(o => $"[[{o}]]"));

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
                body.AppendLine($"PLACEHOLDER_FOR_REVISION_TEXT\r\n");

                var textWithLinks = WordsRepository.GetRandomWords(rand.Next(5, 10));
                textWithLinks.AddRange(GetRandomizedList(recentPageNames).Take(rand.Next(1, 2)).Select(o => $"[[{o}]]"));
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

                controller.SavePage(page);

                recentPageNames = GetRandomizedList(recentPageNames).Take(100).ToList();
            }
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
