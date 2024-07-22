using Microsoft.AspNetCore.Identity;
using NTDLS.Helpers;
using System.Security.Claims;
using System.Text;
using TightWiki.Library;
using TightWiki.Models.DataModels;
using TightWiki.Repository;

namespace DummyPageGenerator
{
    internal class PageGenerator
    {
        private readonly object _lockObject = new();
        private List<Page> _pagePool;
        private readonly Random _random;
        private readonly List<string> _namespaces;
        private readonly List<string> _tags;
        private readonly List<string> _fileNames;
        private List<string> _recentPageNames = new();
        private readonly UserManager<IdentityUser> _userManager;
        private readonly List<AccountProfile> _users;

        public List<AccountProfile> Users => _users;
        public Random Random => _random;

        public PageGenerator(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
            _random = new Random();

            _namespaces = PageRepository.GetAllNamespaces();
            _tags = WordsRepository.GetRandomWords(250);
            _fileNames = WordsRepository.GetRandomWords(50);
            _pagePool = PageRepository.GetAllPages();

            if (_namespaces.Count < 250)
            {
                _namespaces.AddRange(WordsRepository.GetRandomWords(250));
            }

            _users = UsersRepository.GetAllUsers();

            if (_users.Count < 1124)
            {
                for (int i = 0; i < 1124 - _users.Count; i++)
                {
                    string emailAddress = WordsRepository.GetRandomWords(1).First() + "@" + WordsRepository.GetRandomWords(1).First() + ".com";
                }

                _users = UsersRepository.GetAllUsers();
            }
        }

        /// <summary>
        /// Creates a user and the associated profile with claims and such.
        /// </summary>
        /// <param name="emailAddress"></param>
        /// <exception cref="Exception"></exception>
        public void CreateUserAndProfile(string emailAddress)
        {
            var user = new IdentityUser()
            {
                UserName = emailAddress,
                Email = emailAddress
            };

            var result = _userManager.CreateAsync(user, WordsRepository.GetRandomWords(1).First() + Guid.NewGuid().ToString()).Result;
            if (!result.Succeeded)
            {
                throw new Exception(string.Join("\r\n", result.Errors.Select(o => o.Description)));
            }

            var userId = _userManager.GetUserIdAsync(user).Result;
            var membershipConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Membership");

            UsersRepository.CreateProfile(Guid.Parse(userId));

            var claimsToAdd = new List<Claim>
                    {
                        new (ClaimTypes.Role, membershipConfig.Value<string>("Default Signup Role").EnsureNotNull()),
                        new ("timezone", membershipConfig.Value<string>("Default TimeZone").EnsureNotNull()),
                        new (ClaimTypes.Country, membershipConfig.Value<string>("Default Country").EnsureNotNull()),
                        new ("language", membershipConfig.Value<string>("Default Language").EnsureNotNull()),
                    };

            SecurityRepository.UpsertUserClaims(_userManager, user, claimsToAdd);
        }

        /// <summary>
        /// Creates a paragraph/sentence structure.
        /// </summary>
        /// <param name="words"></param>
        /// <returns></returns>
        private string GetParagraph(int words)
        {
            using var client = new HttpClient();

            var response = client.GetAsync($"https://textsauce.com/api/Paragraph/English/{words}").Result;
            response.EnsureSuccessStatusCode();

            return response.Content.ReadAsStringAsync().Result;
        }

        /// <summary>
        /// Creates a paragraph/sentence structure with links and wiki markup.
        /// </summary>
        /// <param name="wordCount"></param>
        /// <returns></returns>
        private string GenerateWikiParagraph(int wordCount)
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
                        var recentPage = GetRandomRecentPageName();
                        if (recentPage != null)
                        {
                            paragraph = paragraph.Replace(token, $"[[{recentPage}]]");
                        }
                        break;
                }
            }

            return paragraph;
        }

        private string? GetRandomRecentPageName()
        {
            lock (_pagePool)
            {
                if (_recentPageNames.Count == 0)
                {
                    return null;
                }

                if (_recentPageNames.Count > 200) //Shuffle and limit the recent page names.
                {
                    _recentPageNames = ShuffleList(_recentPageNames).Take(100).ToList();
                }

                return _recentPageNames[_random.Next(0, _recentPageNames.Count)];
            }
        }

        private List<string> GetRandomRecentPageNames(int count)
        {
            lock (_pagePool)
            {
                if (_recentPageNames.Count > 200) //Shuffle and limit the recent page names.
                {
                    _recentPageNames = ShuffleList(_recentPageNames).Take(100).ToList();
                }

                var pageNames = new List<string>();
                for (int i = 0; i < count; i++)
                {
                    pageNames.Add(_recentPageNames[_random.Next(0, _recentPageNames.Count)]);
                }
                return pageNames;
            }
        }

        private void AddRecentPageName(string pageName)
        {
            lock (_pagePool)
            {
                _recentPageNames.Add(pageName);
            }
        }

        /// <summary>
        /// Creates a random page on the wiki.
        /// </summary>
        /// <param name="userId"></param>
        public void GeneratePage(Guid userId)
        {
            try
            {
                Console.WriteLine($"{userId} is creating a page.");

                var ns = _namespaces[_random.Next(_namespaces.Count)];

                var pageName = ns + " :: " + string.Join(" ", WordsRepository.GetRandomWords(3));

                AddRecentPageName(pageName);

                var body = new StringBuilder();

                body.AppendLine($"##title ##Tag(" + string.Join(' ', ShuffleList(_tags).Take(_random.Next(1, 4))) + ")");
                body.AppendLine($"##toc");

                body.AppendLine($"==Overview");
                body.AppendLine(GenerateWikiParagraph(_random.Next(50, 100)));
                body.AppendLine("\r\n");

                body.AppendLine($"==Revision Section");
                body.AppendLine($"This is here for the workload generator to easily modify the page.");
                body.AppendLine($"PLACEHOLDER_FOR_REVISION_TEXT_BEGIN\r\nPLACEHOLDER_FOR_REVISION_TEXT_END\r\n");

                var textWithLinks = WordsRepository.GetRandomWords(_random.Next(5, 10));
                textWithLinks.AddRange(GetRandomRecentPageNames(_random.Next(1, 2)).Select(o => $"[[{o}]]"));

                if (_random.Next(100) >= 95)
                {
                    //Add dead links (missing pages).
                    textWithLinks.AddRange(WordsRepository.GetRandomWords(_random.Next(1, 2)).Select(o => $"[[{o}]]"));
                }

                body.AppendLine($"==See Also");
                body.AppendLine(string.Join(' ', ShuffleList(textWithLinks)));
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
                    var fileName = _fileNames[_random.Next(_fileNames.Count)] + ".txt"; ;
                    var fileData = Encoding.UTF8.GetBytes(page.Body);
                    AttachFile(newPageId, userId, fileName, fileData);
                }

                InsertPagePool(page);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private Page GetRandomPage()
        {
            lock (_pagePool)
            {
                return _pagePool[_random.Next(0, _pagePool.Count)];
            }
        }

        private void InsertPagePool(Page page)
        {
            lock (_pagePool)
            {
                _pagePool.Add(page);
            }
        }

        /// <summary>
        /// Modifies a random page on the wiki.
        /// </summary>
        /// <param name="userId"></param>
        public void ModifyRandomPages(Guid userId)
        {
            Console.WriteLine($"{userId} is modifying a page.");

            var pageToModify = GetRandomPage();

            AddRecentPageName(pageToModify.Name);

            string beginTag = "PLACEHOLDER_FOR_REVISION_TEXT_BEGIN";
            string endTag = "PLACEHOLDER_FOR_REVISION_TEXT_END";

            int beginIndex = pageToModify.Body.IndexOf(beginTag);
            int endIndex = pageToModify.Body.IndexOf(endTag);

            if (beginIndex > 0 && endIndex > beginIndex)
            {
                string topText = pageToModify.Body.Substring(0, beginIndex + beginTag.Length);
                string bottomText = pageToModify.Body.Substring(endIndex);

                pageToModify.Body = topText.Trim()
                    + "\r\n" + GenerateWikiParagraph(_random.Next(10, 20))
                    + "\r\n" + bottomText.Trim();
                pageToModify.ModifiedByUserId = userId;
                pageToModify.ModifiedByUserId = userId;
                TightWiki.Engine.WikiHelper.UpsertPage(pageToModify);

                if (_random.Next(100) >= 90)
                {
                    var fileName = _fileNames[_random.Next(_fileNames.Count)] + ".txt";
                    var fileData = Encoding.UTF8.GetBytes(pageToModify.Body);
                    AttachFile(pageToModify.Id, userId, fileName, fileData);
                }
            }
        }

        /// <summary>
        /// Attaches a file to a wiki page.
        /// </summary>
        /// <param name="pageId"></param>
        /// <param name="userId"></param>
        /// <param name="fileName"></param>
        /// <param name="fileData"></param>
        private void AttachFile(int pageId, Guid userId, string fileName, byte[] fileData)
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

        /// <summary>
        /// Returns a shuffled version of the input list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        private List<T> ShuffleList<T>(List<T> list)
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

        /// <summary>
        /// Adds some random wiki text to a word.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private string AddWikiMarkup(string text)
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
