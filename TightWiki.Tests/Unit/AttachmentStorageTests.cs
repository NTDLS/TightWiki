using System.Text;
using TightWiki.Library;
using TightWiki.Plugin;
using TightWiki.Plugin.Models;
using TightWiki.Repository;

namespace TightWiki.Tests.Unit
{
    public class AttachmentStorageTests : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "TightWikiAttachmentTests", Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        [Fact]
        public async Task WrittenContentIsReadBack()
        {
            var storage = new AttachmentStorage(_root);
            var content = Encoding.UTF8.GetBytes("hello");

            await storage.WriteAsync(12, 3, "Holiday Video.MP4", content);

            Assert.True(storage.Exists(12, 3));
            Assert.Equal(Path.Combine(_root, "12", "3.mp4"), storage.FindPath(12, 3));
            Assert.Equal(content, await storage.ReadAllBytesAsync(12, 3));
        }

        [Theory]
        [InlineData("report.pdf", ".pdf")]
        [InlineData("Photo.JPEG", ".jpeg")]
        [InlineData("archive.tar.gz", ".gz")]
        [InlineData("README", "")]
        [InlineData("odd.na me", "")]
        [InlineData("dots.", "")]
        [InlineData("long.abcdefghijklmnopq", "")]
        [InlineData("..\\..\\escape.exe", ".exe")]
        public void OnlyPlainExtensionsAreKept(string fileName, string expected)
            => Assert.Equal(expected, AttachmentStorage.GetStoredExtension(fileName));

        [Fact]
        public async Task ANewVersionOfARevisionReplacesTheOldOneWhateverItsExtension()
        {
            var storage = new AttachmentStorage(_root);
            await storage.WriteAsync(9, 1, "clip.mov", [1]);
            await storage.WriteAsync(9, 1, "clip.mp4", [2]);

            Assert.Equal([Path.Combine(_root, "9", "1.mp4")], storage.EnumerateFiles().ToList());
        }

        [Fact]
        public async Task RevisionsAreNotConfusedWithOthersThatStartWithTheSameDigits()
        {
            var storage = new AttachmentStorage(_root);
            await storage.WriteAsync(4, 10, "ten.txt", [10]);
            await storage.WriteAsync(4, 1, "one", [1]);

            Assert.Equal([1], await storage.ReadAllBytesAsync(4, 1));
            Assert.Equal([10], await storage.ReadAllBytesAsync(4, 10));

            storage.Delete(4, 1);
            Assert.True(storage.Exists(4, 10));
        }

        [Fact]
        public async Task MissingContentReadsAsNull()
        {
            var storage = new AttachmentStorage(_root);

            Assert.False(storage.Exists(1, 1));
            Assert.Null(storage.FindPath(1, 1));
            Assert.Null(await storage.ReadAllBytesAsync(1, 1));
        }

        [Fact]
        public async Task DeletingTheLastRevisionRemovesItsDirectory()
        {
            var storage = new AttachmentStorage(_root);
            await storage.WriteAsync(5, 1, "a.png", [1]);
            await storage.WriteAsync(5, 2, "a.png", [2]);

            storage.Delete(5, 1);
            Assert.True(Directory.Exists(Path.Combine(_root, "5")));

            storage.Delete(5, 2);
            Assert.False(Directory.Exists(Path.Combine(_root, "5")));

            storage.Delete(5, 2); //Deleting what is not there is not an error.
        }

        [Fact]
        public async Task MovingARevisionKeepsItsExtensionAndReplacesTheTarget()
        {
            var storage = new AttachmentStorage(_root);
            await storage.WriteAsync(7, 1, "old.gif", [1]);
            await storage.WriteAsync(7, 4, "new.png", [4]);

            storage.MoveRevision(7, 4, 1);

            Assert.False(storage.Exists(7, 4));
            Assert.Equal(Path.Combine(_root, "7", "1.png"), storage.FindPath(7, 1));
            Assert.Equal([4], await storage.ReadAllBytesAsync(7, 1));
            Assert.Single(storage.EnumerateFiles());
        }

        [Fact]
        public async Task EnumerationSkipsUploadsInProgress()
        {
            var storage = new AttachmentStorage(_root);
            await storage.WriteAsync(1, 1, "a.txt", [1]);
            File.WriteAllBytes(Path.Combine(_root, ".incoming", "unfinished.part"), [0]);

            Assert.Equal([storage.FindPath(1, 1)], storage.EnumerateFiles().ToList());
        }
    }

    /// <summary>
    /// Runs an attachment through its whole life against the test databases and checks the files on disk at each step.
    /// </summary>
    [Collection("Markup Tests")]
    public class AttachmentLifecycleTests(TwEngineFixture fixture)
        : IClassFixture<TwEngineFixture>
    {
        [Fact]
        public async Task AttachmentContentFollowsThePage()
        {
            var databaseManager = fixture.Artifacts.DatabaseManager;
            var repository = databaseManager.PageRepository;
            var storage = databaseManager.AttachmentStorage;
            var userId = Guid.NewGuid();

            var pageName = $"Attachment Lifecycle {Guid.NewGuid():N}";
            var navigation = TwNavigation.Clean(pageName);
            var pageId = await repository.UpsertPage(fixture.Artifacts.Engine, fixture.Artifacts.Localizer, new TwPage
            {
                Name = pageName,
                Navigation = navigation,
                Body = "An attachment test page.",
                Description = string.Empty,
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            }, fixture.CreateWikiSession());

            async Task Attach(string text)
            {
                var data = Encoding.UTF8.GetBytes(text);
                await repository.UpsertPageFile(new TwPageFileAttachment
                {
                    PageId = pageId,
                    Name = "notes.txt",
                    FileNavigation = "notes-txt",
                    ContentType = "text/plain",
                    Size = data.Length,
                    CreatedDate = DateTime.UtcNow,
                    Data = data
                }, userId);
            }

            //A new attachment is written to disk, not to the database.
            await Attach("first");
            var first = await repository.GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(navigation, "notes-txt");
            Assert.NotNull(first);
            Assert.Equal(1, first.Revision);
            Assert.Equal("first", Encoding.UTF8.GetString(first.Data));
            Assert.Equal(Path.Combine(storage.RootPath, first.Id.ToString(), "1.txt"), storage.FindPath(first.Id, 1));
            Assert.Equal(0, await repository.PagesFactory.ExecuteScalarAsync<int>(
                "SELECT length(Data) FROM PageFileRevision WHERE PageFileId = @Id AND Revision = 1", new { first.Id }));

            //The same content again is not a new revision.
            await Attach("first");
            Assert.False(storage.Exists(first.Id, 2));

            //Changed content is a new revision with its own file, and the old one is kept.
            await Attach("second");
            var second = await repository.GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(navigation, "notes-txt");
            Assert.NotNull(second);
            Assert.Equal(2, second.Revision);
            Assert.Equal("second", Encoding.UTF8.GetString(second.Data));
            Assert.Equal("first", Encoding.UTF8.GetString(
                (await repository.GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(navigation, "notes-txt", 1))!.Data));

            //Deleting a revision deletes its file.
            await repository.DeletePageAttachmentRevision(first.Id, 1);
            Assert.False(storage.Exists(first.Id, 1));
            Assert.True(storage.Exists(first.Id, 2));

            //Deleting and restoring the page leaves the file alone.
            await repository.MovePageToDeletedById(pageId, userId);
            Assert.True(storage.Exists(first.Id, 2));
            await repository.RestoreDeletedPageByPageId(pageId);
            Assert.True(storage.Exists(first.Id, 2));
            Assert.Equal("second", Encoding.UTF8.GetString(
                (await repository.GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(navigation, "notes-txt"))!.Data));

            //Purging the deleted page deletes the file.
            await repository.MovePageToDeletedById(pageId, userId);
            await repository.PurgeDeletedPageByPageId(pageId);
            Assert.False(storage.Exists(first.Id, 2));
            Assert.False(Directory.Exists(Path.Combine(storage.RootPath, first.Id.ToString())));
        }

        [Fact]
        public async Task OlderAttachmentsAreMovedToTheStorage()
        {
            var databaseManager = fixture.Artifacts.DatabaseManager;
            var repository = databaseManager.PageRepository;
            var storage = databaseManager.AttachmentStorage;
            var userId = Guid.NewGuid();

            var pageName = $"Attachment Migration {Guid.NewGuid():N}";
            var navigation = TwNavigation.Clean(pageName);
            var pageId = await repository.UpsertPage(fixture.Artifacts.Engine, fixture.Artifacts.Localizer, new TwPage
            {
                Name = pageName,
                Navigation = navigation,
                Body = "An attachment migration test page.",
                Description = string.Empty,
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            }, fixture.CreateWikiSession());

            var content = Encoding.UTF8.GetBytes("stored the old way");
            await repository.UpsertPageFile(new TwPageFileAttachment
            {
                PageId = pageId,
                Name = "old.txt",
                FileNavigation = "old-txt",
                ContentType = "text/plain",
                Size = content.Length,
                CreatedDate = DateTime.UtcNow,
                Data = content
            }, userId);

            var attachment = await repository.GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(navigation, "old-txt");
            Assert.NotNull(attachment);

            //Make it look like an attachment from before the storage existed: content in the database, no file.
            await repository.PagesFactory.ExecuteAsync("UPDATE PageFileRevision SET Data = @Data WHERE PageFileId = @Id AND Revision = 1",
                new { Data = content, attachment.Id });
            storage.Delete(attachment.Id, 1);

            //A cancelled run moves nothing and reports what is left.
            var cancelled = await repository.MoveAttachmentContentToStorage(new CancellationToken(canceled: true));
            Assert.Equal(0, cancelled.Moved);
            Assert.True(cancelled.Remaining > 0);
            Assert.False(storage.Exists(attachment.Id, 1));

            var result = await repository.MoveAttachmentContentToStorage();
            Assert.True(result.Moved > 0);
            Assert.Equal(0, result.Failed);
            Assert.Equal(0, result.Remaining);

            Assert.Equal(content, await storage.ReadAllBytesAsync(attachment.Id, 1));
            Assert.EndsWith("1.txt", storage.FindPath(attachment.Id, 1));
            Assert.Equal(0, await repository.PagesFactory.ExecuteScalarAsync<int>(
                "SELECT length(Data) FROM PageFileRevision WHERE PageFileId = @Id AND Revision = 1", new { attachment.Id }));
            Assert.Equal("stored the old way", Encoding.UTF8.GetString(
                (await repository.GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(navigation, "old-txt"))!.Data));

            //Nothing is left, so running it again does nothing.
            Assert.Equal((0, 0, 0), await repository.MoveAttachmentContentToStorage());
        }

        [Fact]
        public async Task BackupsCanIncludeTheAttachments()
        {
            var databaseManager = fixture.Artifacts.DatabaseManager;
            await databaseManager.AttachmentStorage.WriteAsync(999999, 1, "backed up.txt", Encoding.UTF8.GetBytes("backed up"));

            var withAttachments = await databaseManager.BackupDatabases(includeAttachments: true);
            var withoutAttachments = await databaseManager.BackupDatabases();
            try
            {
                using (var zip = System.IO.Compression.ZipFile.OpenRead(Path.Combine(databaseManager.BackupPath, withAttachments.FileName)))
                {
                    Assert.Contains(zip.Entries, o => o.FullName == "pages.db" || o.FullName == "Pages.db");
                    var entry = zip.GetEntry("Attachments/999999/1.txt");
                    Assert.NotNull(entry);
                    using var reader = new StreamReader(entry.Open());
                    Assert.Equal("backed up", reader.ReadToEnd());
                }

                using (var zip = System.IO.Compression.ZipFile.OpenRead(Path.Combine(databaseManager.BackupPath, withoutAttachments.FileName)))
                {
                    Assert.DoesNotContain(zip.Entries, o => o.FullName.StartsWith("Attachments/"));
                }
            }
            finally
            {
                databaseManager.DeleteDatabaseBackup(withAttachments.FileName);
                databaseManager.DeleteDatabaseBackup(withoutAttachments.FileName);
                databaseManager.AttachmentStorage.Delete(999999, 1);
            }
        }

        [Fact]
        public async Task OlderAttachmentsAreStillReadFromTheDatabase()
        {
            //Attachments uploaded before the attachment storage existed only have their content in the database.
            var repository = fixture.Artifacts.DatabaseManager.PageRepository;
            var storage = fixture.Artifacts.DatabaseManager.AttachmentStorage;

            //Only one that is still on a page can be read through a page.
            var legacy = await repository.PagesFactory.QueryFirstOrDefaultAsync<TwPageFileRevisionKey>(
                @"SELECT PFR.PageFileId, PFR.Revision FROM PageFileRevision as PFR
                  INNER JOIN PageFile as PF ON PF.Id = PFR.PageFileId
                  INNER JOIN Page as P ON P.Id = PF.PageId
                  WHERE length(PFR.Data) > 0 LIMIT 1");

            if (legacy == null)
            {
                return; //The seed databases have no attachments on a page that are stored the old way.
            }

            Assert.False(storage.Exists(legacy.PageFileId, legacy.Revision));

            var info = await repository.PagesFactory.QuerySingleAsync<(string PageNavigation, string FileNavigation)>(
                "SELECT P.Navigation, PF.Navigation FROM PageFile as PF INNER JOIN Page as P ON P.Id = PF.PageId WHERE PF.Id = @PageFileId",
                new { legacy.PageFileId });

            var attachment = await repository.GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(
                info.PageNavigation, info.FileNavigation, legacy.Revision);

            Assert.NotNull(attachment);
            Assert.NotEmpty(attachment.Data);
        }
    }
}
