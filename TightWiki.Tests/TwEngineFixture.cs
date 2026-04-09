using Microsoft.Extensions.Configuration;
using NTDLS.Helpers;
using TightWiki.Test.Library;
using Xunit.Abstractions;

namespace TightWiki.Tests
{
    public class TwEngineFixture
    {
        private static readonly Lock _lock = new Lock();
        private static MockWikiEngineArtifacts? _engineArtifacts;

        public MockWikiEngineArtifacts EngineArtifacts => _engineArtifacts.EnsureNotNull();

        public TwEngineFixture()
        {
            if (_engineArtifacts == null)
            {
                lock (_lock)
                {
                    if (_engineArtifacts == null)
                    {
                        var configuration = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
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
    }
}
