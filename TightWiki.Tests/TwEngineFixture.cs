using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NTDLS.Helpers;
using TightWiki.Library;
using TightWiki.Models.DataModels;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;
using TightWiki.Test.Library;
using Xunit.Abstractions;

namespace TightWiki.Tests
{
    public class TwEngineFixture
    {
        private readonly ITestOutputHelper _output;

        private static readonly Lock _lock = new Lock();
        private static readonly string _emailAddress = "Testy@McTestface.net";
        private static readonly string _accountName = "Testy McTestface";
        private static readonly string _password = $"{_accountName}sP@ssW0rD!]";

        private static MockWikiEngineArtifacts? _engineArtifacts;

        public MockWikiEngineArtifacts EngineArtifacts => _engineArtifacts.EnsureNotNull();
        public string UserName => _accountName.EnsureNotNull();

        public TwEngineFixture(ITestOutputHelper output)
        {
            _output = output;

            if (_engineArtifacts == null)
            {
                lock (_lock)
                {
                    if (_engineArtifacts == null)
                    {
                        var configuration = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory()) // important
                            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                            .Build();

                        //Copying prod databases to the test directory to ensure we have the same data for testing.
                        var originalDatabasePath = configuration.GetConnectionString("OriginalDatabasePath").EnsureNotNull();
                        var databasePath = configuration.GetConnectionString("DatabasePath").EnsureNotNull();
                        Directory.CreateDirectory(originalDatabasePath);

                        //Copy all prod-release databases to the test directory.
                        foreach (var file in Directory.GetFiles(originalDatabasePath))
                        {
                            var destPath = Path.Combine(databasePath, Path.GetFileName(file));
                            Directory.CreateDirectory(Path.GetDirectoryName(destPath).EnsureNotNull());
                            File.Copy(file, destPath, overwrite: true);
                        }

                        _engineArtifacts = new MockWikiEngineArtifacts();
                    }
                }
            }
        }

        public ISessionState CreateWikiSession()
        {
            return new DummySessionState();
        }

        public async Task<ITwEngineState> WikiTransform(string pageName, string body)
        {
            var navigation = new NamespaceNavigation(pageName);

            var page = new WikiPage()
            {
                Body = body,
                Name = pageName,
                Navigation = navigation.Canonical
            };

            return await EngineArtifacts.Engine.Transform(EngineArtifacts.Localizer, CreateWikiSession(), page);
        }

        private async Task CreateFixtureInstance()
        {
            _output.WriteLine($"Loadnig all settings.");
            //await ConfigurationRepository.ReloadEverything();

            _output.WriteLine($"Creating users and profiles.");
            await EngineArtifacts.CreateUserAndProfile(_emailAddress, _accountName, _password);

            _output.WriteLine($"Generating test pages.");
            await GenerateTestPages();
        }

        private async Task GenerateTestPages()
        {
            await EngineArtifacts.CreatePage("Test :: Test Include", _accountName, ["Test", "Test Page", "Test Include"]);
        }
    }
}
