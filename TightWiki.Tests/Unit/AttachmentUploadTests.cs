using System.Text;
using TightWiki.Library.Security;
using TightWiki.Plugin;
using TightWiki.Plugin.Models;
using TightWiki.Repository;

namespace TightWiki.Tests.Unit
{
    public class Crc32Tests
    {
        [Fact]
        public async Task TheFastCrc32MatchesTheOriginal()
        {
            //Stored hashes were made with the original implementation, a new upload of the same file must hash the same.
            var random = new Random(42);
            foreach (var length in new[] { 0, 1, 7, 1000, 65537 })
            {
                var bytes = new byte[length];
                random.NextBytes(bytes);

                var original = (int)new Crc32().Get(bytes);
                Assert.Equal(original, SecurityUtility.Crc32(bytes));
                Assert.Equal(original, await SecurityUtility.Crc32Async(new MemoryStream(bytes)));
            }
        }
    }

    public class AttachmentUploadTests : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "TightWikiUploadTests", Guid.NewGuid().ToString("N"));
        private readonly Guid _user = Guid.NewGuid();

        public void Dispose()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        private TwAttachmentUploads CreateUploads() => new(new AttachmentStorage(_root));

        [Fact]
        public async Task PiecesAreAppendedInOrder()
        {
            var uploads = CreateUploads();
            var upload = uploads.Start(_user, 1, "page", "file.bin", 10);

            Assert.Equal(4, await uploads.AppendAsync(upload, 0, new MemoryStream([0, 1, 2, 3]), default));
            Assert.Equal(10, await uploads.AppendAsync(upload, 4, new MemoryStream([4, 5, 6, 7, 8, 9]), default));

            Assert.Equal(Enumerable.Range(0, 10).Select(o => (byte)o), File.ReadAllBytes(uploads.GetPartPath(upload.UploadId)));
        }

        [Fact]
        public async Task APieceAtTheWrongOffsetSaysWhereToContinue()
        {
            var uploads = CreateUploads();
            var upload = uploads.Start(_user, 1, "page", "file.bin", 10);
            await uploads.AppendAsync(upload, 0, new MemoryStream([0, 1, 2, 3]), default);

            //Sending the same piece twice (a retry after a lost response) must not duplicate it.
            var error = await Assert.ThrowsAsync<TwAttachmentUploadException>(() => uploads.AppendAsync(upload, 0, new MemoryStream([0, 1, 2, 3]), default));
            Assert.Equal(409, error.StatusCode);
            Assert.Equal(4, error.Received);
            Assert.Equal(4, uploads.GetReceived(upload.UploadId));
        }

        [Fact]
        public async Task MoreThanTheDeclaredSizeIsRejectedAndNotKept()
        {
            var uploads = CreateUploads();
            var upload = uploads.Start(_user, 1, "page", "file.bin", 5);
            await uploads.AppendAsync(upload, 0, new MemoryStream([0, 1]), default);

            var error = await Assert.ThrowsAsync<TwAttachmentUploadException>(() => uploads.AppendAsync(upload, 2, new MemoryStream([2, 3, 4, 5, 6]), default));
            Assert.Equal(400, error.StatusCode);

            //Only whole pieces are kept, so the piece can be sent again.
            Assert.Equal(2, uploads.GetReceived(upload.UploadId));
        }

        [Fact]
        public void UploadsCanOnlyBeSeenByTheirOwner()
        {
            var uploads = CreateUploads();
            var upload = uploads.Start(_user, 1, "page", "file.bin", 5);

            Assert.Equal(upload.UploadId, uploads.Get(upload.UploadId, _user).UploadId);
            Assert.Equal(404, Assert.Throws<TwAttachmentUploadException>(() => uploads.Get(upload.UploadId, Guid.NewGuid())).StatusCode);
            Assert.Equal(404, Assert.Throws<TwAttachmentUploadException>(() => uploads.Get("..\\..\\config", _user)).StatusCode);
        }

        [Fact]
        public void AnUploadSurvivesARestart()
        {
            var upload = CreateUploads().Start(_user, 1, "page", "file.bin", 5);

            //A new instance, as after the site restarts, still finds it.
            var found = CreateUploads().Get(upload.UploadId, _user);
            Assert.Equal("file.bin", found.FileName);
            Assert.Equal(5, found.Size);
        }

        [Fact]
        public void CancellingRemovesEverything()
        {
            var uploads = CreateUploads();
            var upload = uploads.Start(_user, 1, "page", "file.bin", 5);

            uploads.Delete(upload.UploadId);

            Assert.Throws<TwAttachmentUploadException>(() => uploads.Get(upload.UploadId, _user));
            Assert.Empty(Directory.EnumerateFiles(Path.Combine(_root, ".incoming")));
        }
    }

    [Collection("Markup Tests")]
    public class AttachmentUploadRepositoryTests(TwEngineFixture fixture)
        : IClassFixture<TwEngineFixture>
    {
        [Fact]
        public async Task AReceivedUploadIsMovedIntoPlace()
        {
            var databaseManager = fixture.Artifacts.DatabaseManager;
            var repository = databaseManager.PageRepository;
            var storage = databaseManager.AttachmentStorage;
            var userId = Guid.NewGuid();

            var pageName = $"Attachment Upload {Guid.NewGuid():N}";
            var navigation = TwNavigation.Clean(pageName);
            var pageId = await repository.UpsertPage(fixture.Artifacts.Engine, fixture.Artifacts.Localizer, new TwPage
            {
                Name = pageName,
                Navigation = navigation,
                Body = "An upload test page.",
                Description = string.Empty,
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            }, fixture.CreateWikiSession());

            async Task<(bool Changed, string IncomingPath)> Receive(string text)
            {
                var content = Encoding.UTF8.GetBytes(text);
                var incomingPath = storage.GetIncomingPath($"{Guid.NewGuid():N}.upload.part");
                await File.WriteAllBytesAsync(incomingPath, content);

                var changed = await repository.UpsertPageFileFromIncoming(new TwPageFileAttachment
                {
                    PageId = pageId,
                    Name = "video.mp4",
                    FileNavigation = "video-mp4",
                    ContentType = "video/mp4",
                    Size = content.Length,
                    CreatedDate = DateTime.UtcNow
                }, incomingPath, SecurityUtility.Crc32(content), userId);

                return (changed, incomingPath);
            }

            var first = await Receive("first upload");
            Assert.True(first.Changed);
            Assert.False(File.Exists(first.IncomingPath)); //Moved, not copied.

            var attachment = await repository.GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(navigation, "video-mp4");
            Assert.NotNull(attachment);
            Assert.Equal(Path.Combine(storage.RootPath, attachment.Id.ToString(), "1.mp4"), storage.FindPath(attachment.Id, 1));
            Assert.Equal("first upload", Encoding.UTF8.GetString(attachment.Data));

            //The same content again is not a new revision, and the received file is left for the caller to delete.
            var again = await Receive("first upload");
            Assert.False(again.Changed);
            Assert.True(File.Exists(again.IncomingPath));
            Assert.False(storage.Exists(attachment.Id, 2));
            File.Delete(again.IncomingPath);

            var second = await Receive("second upload");
            Assert.True(second.Changed);
            Assert.Equal("second upload", Encoding.UTF8.GetString(
                (await repository.GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(navigation, "video-mp4"))!.Data));
        }
    }
}
