using TightWiki.Plugin;
using TightWiki.Plugin.Models;

namespace TightWiki.Tests.Unit
{
    [Collection("Markup Tests")]
    public class MediaFunctionTests(TwEngineFixture fixture)
        : IClassFixture<TwEngineFixture>
    {
        private Task<string> CreatePageWithAttachments(params (string Name, string ContentType)[] files)
            => CreateNamedPageWithAttachments($"Media Test {Guid.NewGuid():N}", files);

        private async Task<string> CreateNamedPageWithAttachments(string pageName, params (string Name, string ContentType)[] files)
        {
            var repository = fixture.Artifacts.DatabaseManager.PageRepository;
            var userId = Guid.NewGuid();

            var pageId = await repository.UpsertPage(fixture.Artifacts.Engine, fixture.Artifacts.Localizer, new TwPage
            {
                Name = pageName,
                Navigation = TwNamespaceNavigation.CleanAndValidate(pageName),
                Body = "A media test page.",
                Description = string.Empty,
                CreatedByUserId = userId,
                ModifiedByUserId = userId,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            }, fixture.CreateWikiSession());

            foreach (var (name, contentType) in files)
            {
                await repository.UpsertPageFile(new TwPageFileAttachment
                {
                    PageId = pageId,
                    Name = name,
                    FileNavigation = TwNavigation.Clean(name),
                    ContentType = contentType,
                    Size = 3,
                    CreatedDate = DateTime.UtcNow,
                    Data = [1, 2, 3]
                }, userId);
            }

            return pageName;
        }

        [Fact]
        public async Task VideoIsAPlayerThatStreamsTheAttachment()
        {
            var pageName = await CreatePageWithAttachments(("clip.mp4", "video/mp4"), ("still.png", "image/png"));
            var navigation = TwNavigation.Clean(pageName);

            var html = (await fixture.WikiTransform(pageName, "##Video(clip.mp4, 640, still.png)")).HtmlResult;

            Assert.Contains("<video controls preload=\"metadata\" playsinline class=\"img-fluid\" width=\"640\"", html);
            Assert.Contains($"poster=\"/Page/Binary/{navigation}/still_png\"", html);
            Assert.Contains($"<source src=\"/Page/Binary/{navigation}/clip_mp4\" type=\"video/mp4\" />", html);
            Assert.Contains("</video>", html);
        }

        [Fact]
        public async Task QuickTimeVideoIsPlayedAsMp4WithADownloadLink()
        {
            //Phones and cameras record H.264 in .mov files, which browsers play when told they are MP4.
            var pageName = await CreatePageWithAttachments(("Marlin 1895 45-70 11.MOV", "video/quicktime"));
            var navigation = TwNavigation.Clean(pageName);

            var html = (await fixture.WikiTransform(pageName, "##Video(\"Marlin 1895 45-70 11.MOV\")")).HtmlResult;

            Assert.Contains($"<source src=\"/Page/Binary/{navigation}/marlin_1895_45-70_11_mov\" type=\"video/mp4\" />", html);
            Assert.Contains("If the video does not play:", html);
            Assert.DoesNotContain("can not be played", html);
        }

        [Fact]
        public async Task FileLinkTextIsOptional()
        {
            var pageName = await CreatePageWithAttachments(("notes.txt", "text/plain"));
            var navigation = TwNavigation.Clean(pageName);

            var html = (await fixture.WikiTransform(pageName, "##File(\"notes.txt\")")).HtmlResult;

            Assert.Contains($"<a href=\"/Page/Binary/{navigation}/notes_txt\">notes.txt</a>", html);
        }

        [Fact]
        public async Task AutoplayIsAlwaysMuted()
        {
            //Browsers refuse to start a video with sound by itself.
            var pageName = await CreatePageWithAttachments(("loop.webm", "video/webm"));

            var html = (await fixture.WikiTransform(pageName, "##Video(loop.webm, :loop=true, :autoplay=true)")).HtmlResult;

            Assert.Contains(" loop autoplay muted", html);
        }

        [Fact]
        public async Task FormatsBrowsersCanNotPlayAreOfferedAsADownload()
        {
            var pageName = await CreatePageWithAttachments(("old.avi", "video/x-msvideo"));
            var navigation = TwNavigation.Clean(pageName);

            var html = (await fixture.WikiTransform(pageName, "##Video(old.avi)")).HtmlResult;

            Assert.DoesNotContain("<video", html);
            Assert.Contains("This file can not be played in the browser.", html);
            Assert.Contains($"<a href=\"/Page/Binary/{navigation}/old_avi\">Download old.avi</a>", html);
        }

        [Fact]
        public async Task AudioIsAPlayer()
        {
            var pageName = await CreatePageWithAttachments(("song.mp3", "audio/mpeg"));
            var navigation = TwNavigation.Clean(pageName);

            var html = (await fixture.WikiTransform(pageName, "##Audio(song.mp3, :loop=true)")).HtmlResult;

            Assert.Contains($"<audio controls preload=\"metadata\" loop><source src=\"/Page/Binary/{navigation}/song_mp3\" type=\"audio/mpeg\" />", html);
        }

        [Fact]
        public async Task PagesInANamespaceCanReferToEachOtherByShortName()
        {
            var ns = $"Ns{Guid.NewGuid():N}";
            await CreateNamedPageWithAttachments($"{ns} :: Gallery", ("logo.png", "image/png"), ("guide.pdf", "application/pdf"));
            var galleryNavigation = TwNamespaceNavigation.CleanAndValidate($"{ns} :: Gallery");

            var html = (await fixture.WikiTransform($"{ns} :: Article", "##Image(Gallery/logo.png)\n##File(Gallery/guide.pdf, The guide)")).HtmlResult;

            Assert.Contains($"src=\"/Page/Image/{galleryNavigation}/logo_png\"", html);
            Assert.Contains($"<a href=\"/Page/Binary/{galleryNavigation}/guide_pdf\">The guide</a>", html);
        }

        [Fact]
        public async Task AFullyNamedPageIsNotRedirectedToTheNamespace()
        {
            //"Other :: Gallery/logo.png" names its namespace, so a missing file is reported rather than looked for elsewhere.
            var ns = $"Ns{Guid.NewGuid():N}";
            await CreateNamedPageWithAttachments($"{ns} :: Gallery", ("logo.png", "image/png"));

            var html = (await fixture.WikiTransform($"{ns} :: Article", "##File(Other :: Gallery/logo.png, x)")).HtmlResult;

            Assert.Contains("File not found [Other :: Gallery/logo.png]", html);
        }

        [Fact]
        public async Task ImageAttributesCanNotAddMarkup()
        {
            var pageName = await CreatePageWithAttachments(("logo.png", "image/png"));

            var html = (await fixture.WikiTransform(pageName, "##Image(logo.png, 50, \"a\\\" onerror=\\\"alert(1)\", \"x\\\" onclick=\\\"y\")")).HtmlResult;

            Assert.DoesNotContain("\" onerror=\"", html);
            Assert.DoesNotContain("\" onclick=\"", html);
            Assert.Contains("?Scale=50\"", html);
        }

        [Fact]
        public async Task FileLinkTextCanNotAddMarkup()
        {
            var pageName = await CreatePageWithAttachments(("notes.txt", "text/plain"));

            var html = (await fixture.WikiTransform(pageName, "##File(notes.txt, \"Tom & Jerry\")")).HtmlResult;

            Assert.Contains(">Tom &amp; Jerry</a>", html);
        }

        [Fact]
        public async Task ErrorsAreShownAsText()
        {
            var pageName = await CreatePageWithAttachments();

            var html = (await fixture.WikiTransform(pageName, "##File(\"<script>alert(1)</script>.txt\", x)")).HtmlResult;

            Assert.DoesNotContain("<script>alert(1)</script>", html);
            Assert.Contains("File not found", html);
            Assert.DoesNotContain("</font></a>", html);
        }

        [Fact]
        public async Task FileNamesCanNotAddMarkup()
        {
            //An attachment's name is shown in its download link.
            var pageName = await CreatePageWithAttachments(("Tom & Jerry.avi", "video/x-msvideo"));
            var html = (await fixture.WikiTransform(pageName, "##Video(Tom & Jerry.avi)")).HtmlResult;
            Assert.Contains("Download Tom &amp; Jerry.avi</a>", html);

            //A name that is not found is shown in the error.
            html = (await fixture.WikiTransform(pageName, "##Video(\"<script>x</script>.mp4\")")).HtmlResult;
            Assert.DoesNotContain("<script>x</script>", html);
        }
    }
}
