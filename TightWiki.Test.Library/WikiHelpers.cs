using Microsoft.AspNetCore.Identity;
using NTDLS.Helpers;
using System.Security.Claims;
using System.Text;
using TightWiki.Library;
using TightWiki.Library.Dummy;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Models;
using Xunit.Abstractions;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Test.Library
{
    public class WikiHelpers(ITwEngine engine, ITwSharedLocalizationText localizer, ITestOutputHelper output,
        ITwDatabaseManager databaseManager, UserManager<IdentityUser> userManager)
    {
        private static readonly string _emailAddress = "Testy@McTestface.net";
        private static readonly string _accountName = "Testy McTestface";
        private static readonly string _password = $"{_accountName}sP@ssW0rD!]";
        public string UserName => _accountName.EnsureNotNull();

        public ITwSessionState CreateWikiSession()
        {
            return new TwDummySessionState();
        }

        public async Task<ITwEngineState> WikiTransform(string pageName, string body)
        {
            var navigation = new TwNamespaceNavigation(pageName);

            var page = new TwPage()
            {
                Body = body,
                Name = pageName,
                Navigation = navigation.Canonical
            };

            return await engine.Transform(localizer, CreateWikiSession(), page);
        }

        private async Task CreateFixtureInstance()
        {
            output.WriteLine($"Loadnig all settings.");
            //await ConfigurationRepository.ReloadEverything();

            output.WriteLine($"Creating users and profiles.");
            await CreateUserAndProfile(_emailAddress, _accountName, _password);

            output.WriteLine($"Generating test pages.");
            await GenerateTestPages();
        }

        private async Task GenerateTestPages()
        {
            await CreatePage("Test :: Test Include", _accountName, ["Test", "Test Page", "Test Include"]);
        }

        public async Task CreatePage(string navigation, string createdByAccountNavigaion, List<string>? tags = null)
        {
            try
            {
                var profile = await databaseManager.UsersRepository.GetAccountProfileByNavigation(TwNavigation.Clean(createdByAccountNavigaion))
                    ?? throw new Exception($"Could not find the account profile for navigation {createdByAccountNavigaion}");

                var body = new StringBuilder();

                body.AppendLine($"##title");
                if (tags?.Count > 0)
                {
                    body.AppendLine($"$##Tag(\"{string.Join("\",\"", tags)})\"");
                }
                body.AppendLine($"##toc");

                body.AppendLine($"==Overview");
                body.AppendLine($"The {navigation} page.");
                body.AppendLine("\r\n");

                body.AppendLine($"==Details");
                body.AppendLine($"This is a test page.");

                body.AppendLine($"==Related");
                body.AppendLine($"##related");
                body.AppendLine("\r\n");

                var page = new TwPage()
                {
                    Name = navigation,
                    Body = body.ToString(),
                    CreatedByUserId = profile.UserId,
                    ModifiedByUserId = profile.UserId,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                    Description = "This is just a test page.",
                };

                var localizer = new TwVerbatimLocalizationText();
                int newPageId = await databaseManager.PageRepository.UpsertPage(engine, localizer, page);

                var fileName = "testFile.txt";
                var fileData = Encoding.UTF8.GetBytes(page.Body);
                await AttachFile(newPageId, profile.UserId, fileName, fileData);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task AttachFile(int pageId, Guid userId, string fileName, byte[] fileData)
        {
            //if (fileData.Length > GlobalConfiguration.MaxAttachmentFileSize)
            {
                //    throw new Exception("Could not save the attached file, too large");
            }

            await databaseManager.PageRepository.UpsertPageFile(new TwPageFileAttachment()
            {
                Data = fileData,
                CreatedDate = DateTime.UtcNow,
                PageId = pageId,
                Name = fileName,
                FileNavigation = TwNavigation.Clean(fileName),
                Size = fileData.Length,
                ContentType = Utility.GetMimeType(fileName)
            }, userId);
        }

        public async Task CreateUserAndProfile(string emailAddress, string accountName, string password)
        {
            var user = new IdentityUser()
            {
                UserName = emailAddress,
                Email = emailAddress
            };

            var result = userManager.CreateAsync(user, password).Result;
            if (!result.Succeeded)
            {
                throw new Exception(string.Join("\r\n", result.Errors.Select(o => o.Description)));
            }

            var userId = userManager.GetUserIdAsync(user).Result;
            var membershipConfig = await databaseManager.ConfigurationRepository.GetConfigurationEntryValuesByGroupName(TwConfigGroup.Membership);

            await databaseManager.UsersRepository.CreateProfile(Guid.Parse(userId), accountName);

            var claimsToAdd = new List<Claim>
                    {
                        new (ClaimTypes.Role, membershipConfig.Value<string>("Default Signup Role").EnsureNotNull()),
                        new ("timezone", membershipConfig.Value<string>("Default TimeZone").EnsureNotNull()),
                        new (ClaimTypes.Country, membershipConfig.Value<string>("Default Country").EnsureNotNull()),
                        new ("language", membershipConfig.Value<string>("Default Language").EnsureNotNull()),
                    };

            await databaseManager.UsersRepository.UpsertUserClaims(userManager, user, claimsToAdd);
        }
    }
}
