using System.Reflection;
using TightWiki.Plugin;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Repository;

namespace TightWiki.Tests.Unit
{
    [Collection("Markup Tests")]
    public class SearchPermissionTests(TwEngineFixture fixture)
        : IClassFixture<TwEngineFixture>
    {
        /// <summary>A session that is allowed everything, except for pages for which <paramref name="denyWhen"/> is true.</summary>
        private class SelectiveSession : DispatchProxy
        {
            public ITwSessionState Inner { get; set; } = null!;
            public Func<string?, bool> DenyWhen { get; set; } = _ => false;

            protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            {
                if (targetMethod?.Name == nameof(ITwSessionState.HoldsPermission) && args?.Length == 2 && args[0] is string or null)
                {
                    return Task.FromResult(!DenyWhen(args[0] as string));
                }
                return targetMethod?.Invoke(Inner, args);
            }
        }

        private ITwSessionState CreateSession(Func<string?, bool> denyWhen)
        {
            var session = DispatchProxy.Create<ITwSessionState, SelectiveSession>();
            var proxy = (SelectiveSession)(object)session;
            proxy.Inner = fixture.CreateWikiSession();
            proxy.DenyWhen = denyWhen;
            return session;
        }

        private ITwPageRepository Repository => fixture.Artifacts.DatabaseManager.PageRepository;

        [Fact]
        public async Task WithoutASessionNothingIsReturned()
        {
            var pages = await TwSearchResults.GetReadablePages(null, Repository, ["test"]);
            Assert.Empty(pages);
        }

        [Fact]
        public async Task ASessionThatCanReadEverythingSeesEveryMatch()
        {
            var all = await Repository.PageSearch(["test"]);
            Assert.NotEmpty(all);

            var readable = await TwSearchResults.GetReadablePages(CreateSession(_ => false), Repository, ["test"]);
            Assert.Equal(all.Select(o => o.Id), readable.Select(o => o.Id));
        }

        [Fact]
        public async Task ASessionThatCanReadNothingSeesNothing()
        {
            Assert.NotEmpty(await Repository.PageSearch(["test"]));

            var readable = await TwSearchResults.GetReadablePages(CreateSession(_ => true), Repository, ["test"]);
            Assert.Empty(readable);
        }

        [Fact]
        public async Task OnlyThePagesThatCannotBeReadAreRemoved()
        {
            var all = await Repository.PageSearch(["test"]);
            var denied = all.First().Navigation;

            var readable = await TwSearchResults.GetReadablePages(CreateSession(o => o == denied), Repository, ["test"]);

            Assert.DoesNotContain(readable, o => o.Navigation == denied);
            Assert.Equal(all.Count - 1, readable.Count);
        }

        [Fact]
        public async Task SearchIgnoresAccentsAndPunctuation()
        {
            var plain = await Repository.PageSearch(["test"]);
            Assert.NotEmpty(plain);

            var decorated = await Repository.PageSearch(["tést,", "(test)"]);
            Assert.Equal(plain.Select(o => o.Id).Order(), decorated.Select(o => o.Id).Order());
        }
    }
}
