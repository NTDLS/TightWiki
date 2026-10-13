using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;
using TightWiki.TranslationSite.Services;

namespace TightWiki.Tests.Unit
{
    /// <summary>
    /// Tests of the services behind the translations site (translations.tightwiki.com).
    /// </summary>
    public class TranslationSiteTests : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "TwTranslationSiteTests_" + Guid.NewGuid().ToString("N"));

        public TranslationSiteTests()
        {
            Directory.CreateDirectory(_directory);
        }

        public void Dispose()
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            try { Directory.Delete(_directory, true); } catch { /* A temporary folder is not worth failing a test over. */ }
            GC.SuppressFinalize(this);
        }

        internal static string FindTranslations()
        {
            for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
            {
                var candidate = Path.Combine(directory.FullName, "TightWiki", "Translations", "Translations.db");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            throw new FileNotFoundException("Could not find TightWiki/Translations/Translations.db above the test folder.");
        }

        private static readonly Lazy<TranslationCatalog> _catalog = new(() => new TranslationCatalog(FindTranslations()));

        private static TranslationCatalog Catalog => _catalog.Value;

        private sealed class ManualTime : TimeProvider
        {
            private DateTimeOffset _now = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);
            public override DateTimeOffset GetUtcNow() => _now;
            public void Advance(TimeSpan span) => _now += span;
        }

        private SuggestionService CreateService(ManualTime? time = null, Action<SiteOptions>? configure = null)
        {
            var options = new SiteOptions { SuggestionsPath = Path.Combine(_directory, Guid.NewGuid().ToString("N") + ".db") };
            configure?.Invoke(options);
            return new SuggestionService(Catalog, Options.Create(options), time ?? new ManualTime());
        }

        /// <summary>
        /// A phrase and language that has a translation, so that the "already the current translation" rule can be tested.
        /// </summary>
        private static (PhraseEntry Phrase, string Language, string Current) TranslatedPhrase()
        {
            var phrase = Catalog.Phrases.First(o => o.Translations.ContainsKey("German") && !o.English.Contains('{') && o.English.Length < 40);
            return (phrase, "German", phrase.Translations["German"]);
        }

        #region Catalog.

        [Fact]
        public void CatalogLoadsEveryPhraseAndLanguage()
        {
            Assert.True(Catalog.PhraseCount > 900);
            Assert.Equal(46, Catalog.Languages.Count);
            Assert.DoesNotContain(Catalog.Languages, o => o.Name == "English");
            Assert.Contains(Catalog.Languages, o => o.Name == "Hebrew" && o.IsRtl);
            Assert.Contains(Catalog.Languages, o => o.Name == "German" && !o.IsRtl);
        }

        [Fact]
        public void PhraseKeysAreUniqueAndRoundTrip()
        {
            Assert.Equal(Catalog.PhraseCount, Catalog.Phrases.Select(o => o.Key).Distinct().Count());

            foreach (var phrase in Catalog.Phrases.Take(50))
            {
                Assert.Same(phrase, Catalog.FindByKey(phrase.Key));
                Assert.Same(phrase, Catalog.FindByEnglish(phrase.English.ToUpperInvariant()));
            }
        }

        [Fact]
        public void SearchFindsPhrasesByEnglish()
        {
            var results = Catalog.Search("orphaned attachments", null, 0, 50);

            Assert.NotEmpty(results.Items);
            Assert.All(results.Items, o => Assert.Contains("orphaned", o.Phrase.English, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void SearchFindsPhrasesByTheirTranslationInAnyLanguage()
        {
            var (phrase, _, current) = TranslatedPhrase();

            var results = Catalog.Search(current, null, 0, 1000);

            Assert.Contains(results.Items, o => o.Phrase == phrase && o.MatchedLanguages.Contains("German"));
        }

        [Fact]
        public void SearchIgnoresCaseAndAccents()
        {
            var accented = Catalog.Phrases
                .SelectMany(p => p.Translations.Select(t => (Phrase: p, Language: t.Key, Text: t.Value)))
                .First(o => o.Language == "French" && o.Text.Contains('é') && o.Text.Length < 40);

            var results = Catalog.Search(accented.Text.Replace('é', 'e').ToUpperInvariant(), "French", 0, 1000);

            Assert.Contains(results.Items, o => o.Phrase == accented.Phrase);
        }

        [Fact]
        public void SearchByLanguageOnlyLooksAtThatLanguage()
        {
            var (phrase, _, current) = TranslatedPhrase();

            //The German text is not in the French column (and is not English), so a French search must not find it by that text.
            var results = Catalog.Search(current, "French", 0, 1000);

            Assert.DoesNotContain(results.Items, o => o.Phrase == phrase && o.MatchedLanguages.Contains("German"));
        }

        [Fact]
        public void SearchRequiresEveryWord()
        {
            var all = Catalog.Search("database", null, 0, 1000).Total;
            var narrowed = Catalog.Search("database backups", null, 0, 1000).Total;
            var impossible = Catalog.Search("database zzzzqqqq", null, 0, 1000).Total;

            Assert.True(all >= narrowed);
            Assert.True(narrowed > 0);
            Assert.Equal(0, impossible);
        }

        [Fact]
        public void EmptySearchReturnsEverythingInPages()
        {
            var first = Catalog.Search("", null, 0, 20);
            var second = Catalog.Search("   ", null, 20, 20);

            Assert.Equal(Catalog.PhraseCount, first.Total);
            Assert.Equal(20, first.Items.Count);
            Assert.Equal(20, second.Items.Count);
            Assert.Empty(first.Items.Select(o => o.Phrase).Intersect(second.Items.Select(o => o.Phrase)));
        }

        [Fact]
        public void GapsOnlyContainMissingTranslations()
        {
            foreach (var gap in Catalog.GetGaps(50))
            {
                Assert.DoesNotContain(gap.Language.Name, gap.Phrase.Translations.Keys);
            }
        }

        #endregion

        #region Suggestions.

        [Fact]
        public void ASuggestionIsShownAndStartsWithTheSuggestersVote()
        {
            var service = CreateService();
            var (phrase, language, _) = TranslatedPhrase();

            var result = service.Submit("alice", phrase.English, language, "Ein völlig neuer Vorschlag");

            Assert.True(result.Ok, result.Error);
            Assert.False(result.WasDuplicate);
            Assert.Equal(1, result.Suggestion!.UpVotes);
            Assert.Equal(1, result.Suggestion.MyVote);

            var recent = service.GetRecent("bob", 10);
            var shown = Assert.Single(recent);
            Assert.Equal("Ein völlig neuer Vorschlag", shown.Text);
            Assert.Equal(0, shown.MyVote);
        }

        [Fact]
        public void SuggestionsAreSavedForNextTime()
        {
            var path = Path.Combine(_directory, "persist.db");
            var (phrase, language, _) = TranslatedPhrase();

            new SuggestionService(Catalog, Options.Create(new SiteOptions { SuggestionsPath = path }), new ManualTime())
                .Submit("alice", phrase.English, language, "Bleibt erhalten");

            var reopened = new SuggestionService(Catalog, Options.Create(new SiteOptions { SuggestionsPath = path }), new ManualTime());

            Assert.Equal("Bleibt erhalten", Assert.Single(reopened.GetRecent("bob", 10)).Text);
        }

        [Fact]
        public void AnUnwritableDatabaseIsReportedWithWhatToFix()
        {
            var path = Path.Combine(_directory, "readonly.db");
            var created = new SuggestionService(Catalog, Options.Create(new SiteOptions { SuggestionsPath = path }), new ManualTime());
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

            File.SetAttributes(path, FileAttributes.ReadOnly);
            try
            {
                var ex = Assert.Throws<InvalidOperationException>(
                    () => new SuggestionService(Catalog, Options.Create(new SiteOptions { SuggestionsPath = path }), new ManualTime()));

                Assert.Contains(path, ex.Message);
                Assert.Contains(Environment.UserName, ex.Message); //which account needs the permission
                Assert.Contains("Modify", ex.Message);
            }
            finally
            {
                File.SetAttributes(path, FileAttributes.Normal);
            }

            Assert.NotNull(created);
        }

        [Fact]
        public void BadSuggestionsAreRejectedWithAReason()
        {
            var service = CreateService(configure: o => o.MaxSuggestionLength = 40);
            var (phrase, language, current) = TranslatedPhrase();

            Assert.False(service.Submit("alice", "This phrase does not exist anywhere", language, "x").Ok);
            Assert.False(service.Submit("alice", phrase.English, "Klingon", "x").Ok);
            Assert.False(service.Submit("alice", phrase.English, language, "   ").Ok);
            Assert.False(service.Submit("alice", phrase.English, language, new string('a', 41)).Ok);
            Assert.False(service.Submit("alice", phrase.English, language, "two\r\nlines").Ok);

            //The wrong alphabet for the language is caught by the same validator that is used to produce the translations.
            var wrongAlphabet = service.Submit("alice", phrase.English, language, "Сиротские вложения");
            Assert.False(wrongAlphabet.Ok);
            Assert.Contains("Cyrillic", wrongAlphabet.Error);

            var alreadyCurrent = service.Submit("alice", phrase.English, language, current.ToUpperInvariant());
            Assert.False(alreadyCurrent.Ok);
            Assert.Contains("current translation", alreadyCurrent.Error);

            Assert.Empty(service.GetRecent("alice", 10));
        }

        [Fact]
        public void SuggestionsWithBrokenPlaceholdersAreRejected()
        {
            var service = CreateService();
            var phrase = Catalog.Phrases.First(o => o.English.Contains("{0}") && o.Translations.ContainsKey("German"));

            var result = service.Submit("alice", phrase.English, "German", "Ohne Platzhalter");

            Assert.False(result.Ok);
            Assert.Contains("placeholders", result.Error);
        }

        [Fact]
        public void SuggestingTheSameTextAgainCountsAsAVoteNotADuplicate()
        {
            var service = CreateService();
            var (phrase, language, _) = TranslatedPhrase();

            var first = service.Submit("alice", phrase.English, language, "Gleicher Vorschlag");
            var second = service.Submit("bob", phrase.English, language, "gleicher vorschlag");
            var again = service.Submit("alice", phrase.English, language, "Gleicher Vorschlag");

            Assert.True(second.Ok);
            Assert.True(second.WasDuplicate);
            Assert.True(again.WasDuplicate);

            var only = Assert.Single(service.GetRecent("carol", 10));
            Assert.Equal(first.Suggestion!.Id, only.Id);
            Assert.Equal(2, only.UpVotes); //alice's own vote and bob's, but not alice a second time.
        }

        #endregion

        #region Votes.

        [Fact]
        public void VotesCanBeCastChangedAndTakenBack()
        {
            var service = CreateService();
            var (phrase, language, _) = TranslatedPhrase();
            var id = service.Submit("alice", phrase.English, language, "Ein Vorschlag").Suggestion!.Id;

            Assert.Equal(1, service.Vote("bob", id, 1).MyVote);
            Assert.Equal(2, service.GetById("bob", id)!.UpVotes);

            Assert.Equal(-1, service.Vote("bob", id, -1).MyVote); //changed their mind
            var changed = service.GetById("bob", id)!;
            Assert.Equal(1, changed.UpVotes);
            Assert.Equal(1, changed.DownVotes);
            Assert.Equal(0, changed.Score);

            Assert.Equal(0, service.Vote("bob", id, -1).MyVote); //voting the same way again takes it back
            var taken = service.GetById("bob", id)!;
            Assert.Equal(1, taken.UpVotes);
            Assert.Equal(0, taken.DownVotes);
        }

        [Fact]
        public void EachVisitorOnlyVotesOnce()
        {
            var service = CreateService();
            var (phrase, language, _) = TranslatedPhrase();
            var id = service.Submit("alice", phrase.English, language, "Ein Vorschlag").Suggestion!.Id;

            service.Vote("bob", id, 1);
            service.Vote("carol", id, 1);

            Assert.Equal(3, service.GetById("dave", id)!.UpVotes);
        }

        [Fact]
        public void YouCannotVoteOnYourOwnSuggestionOrWithAnInvalidVote()
        {
            var service = CreateService();
            var (phrase, language, _) = TranslatedPhrase();
            var id = service.Submit("alice", phrase.English, language, "Ein Vorschlag").Suggestion!.Id;

            Assert.False(service.Vote("alice", id, -1).Ok);
            Assert.False(service.Vote("bob", id, 5).Ok);
            Assert.False(service.Vote("bob", 987654321, 1).Ok);

            var unchanged = service.GetById("alice", id)!;
            Assert.Equal(1, unchanged.UpVotes);
            Assert.Equal(0, unchanged.DownVotes);
        }

        [Fact]
        public void SuggestionsWithEnoughDownVotesAreHiddenUnlessApproved()
        {
            var service = CreateService();
            var (phrase, language, _) = TranslatedPhrase();
            var id = service.Submit("alice", phrase.English, language, "Ein schlechter Vorschlag").Suggestion!.Id;

            //Score starts at 1, and five more down-votes makes it -4, which is still shown.
            for (int i = 0; i < 5; i++)
            {
                service.Vote("voter" + i, id, -1);
            }
            Assert.Single(service.GetRecent("zed", 10));

            //The sixth takes it to -5, which is the point that it is hidden.
            service.Vote("voter5", id, -1);
            Assert.Empty(service.GetRecent("zed", 10));
            Assert.Empty(service.GetForPhrases("zed", [phrase.English], language));

            //An administrator can bring it back whatever the votes say.
            service.SetStatus(id, SuggestionStatus.Approved);
            Assert.Single(service.GetRecent("zed", 10));
        }

        [Fact]
        public void HiddenSuggestionsCannotBeVotedOn()
        {
            var service = CreateService();
            var (phrase, language, _) = TranslatedPhrase();
            var id = service.Submit("alice", phrase.English, language, "Ein Vorschlag").Suggestion!.Id;

            service.SetStatus(id, SuggestionStatus.Hidden);

            Assert.False(service.Vote("bob", id, 1).Ok);
            Assert.Empty(service.GetRecent("bob", 10));
        }

        [Fact]
        public void VoteNowOnlyOffersSuggestionsTheVisitorHasNotSeenAndDidNotWrite()
        {
            var service = CreateService();
            var phrases = Catalog.Phrases.Where(o => o.Translations.ContainsKey("German") && !o.English.Contains('{')).Take(3).ToList();

            var mine = service.Submit("bob", phrases[0].English, "German", "Von mir").Suggestion!.Id;
            var voted = service.Submit("alice", phrases[1].English, "German", "Schon abgestimmt").Suggestion!.Id;
            var fresh = service.Submit("alice", phrases[2].English, "German", "Noch nicht gesehen").Suggestion!.Id;
            service.Vote("bob", voted, 1);

            var offered = service.GetNeedingVotes("bob", 10);

            Assert.Equal(fresh, Assert.Single(offered).Id);
            Assert.DoesNotContain(offered, o => o.Id == mine || o.Id == voted);
        }

        [Fact]
        public void VoteNowStartsWithTheLeastVotedSuggestion()
        {
            var service = CreateService();
            var phrases = Catalog.Phrases.Where(o => o.Translations.ContainsKey("German") && !o.English.Contains('{')).Take(2).ToList();

            var popular = service.Submit("alice", phrases[0].English, "German", "Beliebt").Suggestion!.Id;
            var lonely = service.Submit("alice", phrases[1].English, "German", "Einsam").Suggestion!.Id;
            service.Vote("carol", popular, 1);
            service.Vote("dave", popular, 1);

            Assert.Equal(lonely, service.GetNeedingVotes("bob", 2)[0].Id);
        }

        #endregion

        #region Limits and events.

        [Fact]
        public void SuggestionsAreRateLimitedPerVisitor()
        {
            var time = new ManualTime();
            var service = CreateService(time, o => o.SuggestionsPerHour = 2);
            var phrases = Catalog.Phrases.Where(o => o.Translations.ContainsKey("German") && !o.English.Contains('{')).Take(4).ToList();

            Assert.True(service.Submit("alice", phrases[0].English, "German", "Eins").Ok);
            Assert.True(service.Submit("alice", phrases[1].English, "German", "Zwei").Ok);

            var third = service.Submit("alice", phrases[2].English, "German", "Drei");
            Assert.False(third.Ok);
            Assert.Contains("try again", third.Error);

            Assert.True(service.Submit("bob", phrases[2].English, "German", "Drei").Ok); //a different visitor is not affected

            time.Advance(TimeSpan.FromMinutes(61));
            Assert.True(service.Submit("alice", phrases[3].English, "German", "Vier").Ok);
        }

        [Fact]
        public void VotesAreRateLimitedPerVisitor()
        {
            var time = new ManualTime();
            var service = CreateService(time, o => o.VotesPerMinute = 2);
            var phrases = Catalog.Phrases.Where(o => o.Translations.ContainsKey("German") && !o.English.Contains('{')).Take(3).ToList();
            var ids = phrases.Select((p, i) => service.Submit("alice" + i, p.English, "German", "Vorschlag " + i).Suggestion!.Id).ToList();

            Assert.True(service.Vote("bob", ids[0], 1).Ok);
            Assert.True(service.Vote("bob", ids[1], 1).Ok);
            Assert.False(service.Vote("bob", ids[2], 1).Ok);

            time.Advance(TimeSpan.FromSeconds(61));
            Assert.True(service.Vote("bob", ids[2], 1).Ok);
        }

        [Fact]
        public void ChangesAreAnnouncedSoOpenPagesCanRefresh()
        {
            var service = CreateService();
            var (phrase, language, _) = TranslatedPhrase();
            var changes = 0;
            service.Changed += () => changes++;

            var id = service.Submit("alice", phrase.English, language, "Ein Vorschlag").Suggestion!.Id;
            service.Vote("bob", id, 1);
            service.SetStatus(id, SuggestionStatus.Approved);
            service.Delete(id);

            Assert.Equal(4, changes);
        }

        [Fact]
        public void StatsCountWhatIsShown()
        {
            var time = new ManualTime();
            var service = CreateService(time);
            var phrases = Catalog.Phrases.Where(o => o.Translations.ContainsKey("German") && !o.English.Contains('{')).Take(2).ToList();

            var old = service.Submit("alice", phrases[0].English, "German", "Alt").Suggestion!.Id;
            time.Advance(TimeSpan.FromDays(10));
            service.Submit("alice", phrases[1].English, "German", "Neu");
            service.Vote("bob", old, 1);

            var stats = service.GetStats();

            Assert.Equal(2, stats.Suggestions);
            Assert.Equal(1, stats.SuggestionsThisWeek);
            Assert.Equal(3, stats.Votes);
            Assert.Equal(2, stats.VotesThisWeek);
        }

        #endregion

        #region Export.

        [Fact]
        public void ExportHasTheBestSuggestionPerPhraseAndLanguageAboveTheMinimumScore()
        {
            var service = CreateService();
            var phrases = Catalog.Phrases.Where(o => o.Translations.ContainsKey("German") && !o.English.Contains('{')).Take(3).ToList();

            var good = service.Submit("alice", phrases[0].English, "German", "Gut").Suggestion!.Id;
            var better = service.Submit("alice", phrases[0].English, "German", "Besser").Suggestion!.Id;
            var unpopular = service.Submit("alice", phrases[1].English, "German", "Unbeliebt").Suggestion!.Id;
            var hidden = service.Submit("alice", phrases[2].English, "German", "Versteckt").Suggestion!.Id;

            foreach (var voter in new[] { "v1", "v2", "v3" })
            {
                service.Vote(voter, good, 1);
                service.Vote(voter, hidden, 1);
            }

            foreach (var voter in new[] { "v1", "v2", "v3", "v4" })
            {
                service.Vote(voter, better, 1);
            }

            service.SetStatus(hidden, SuggestionStatus.Hidden);

            var top = service.GetTopSuggestions(3);

            var only = Assert.Single(top);
            Assert.Equal("Besser", only.Text);
            Assert.Equal(5, only.Score);
            Assert.DoesNotContain(top, o => o.Id == unpopular || o.Id == hidden || o.Id == good);
        }

        #endregion

        #region Turnstile.

        private sealed class FakeCloudflare(string json, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
        {
            public int Calls { get; private set; }
            public string? LastBody { get; private set; }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Calls++;
                LastBody = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
                return new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
            }
        }

        private static TurnstileVerifier CreateVerifier(FakeCloudflare handler, string secret = "a-secret", string[]? hostnames = null)
            => new(new HttpClient(handler),
                Options.Create(new TurnstileOptions { SiteKey = "a-key", SecretKey = secret, AllowedHostnames = hostnames ?? ["translations.tightwiki.com"] }),
                NullLogger<TurnstileVerifier>.Instance);

        [Fact]
        public async Task TurnstileAcceptsAGoodTokenForOurActionAndHost()
        {
            var handler = new FakeCloudflare("""{"success":true,"hostname":"Translations.TightWiki.com","action":"verify"}""");

            Assert.True(await CreateVerifier(handler).VerifyAsync("token", "203.0.113.5"));

            Assert.Contains("secret=a-secret", handler.LastBody);
            Assert.Contains("response=token", handler.LastBody);
            Assert.Contains("remoteip=203.0.113.5", handler.LastBody);
        }

        [Theory]
        [InlineData("""{"success":false,"error-codes":["invalid-input-response"]}""")]
        [InlineData("""{"success":true,"hostname":"translations.tightwiki.com","action":"signup"}""")]
        [InlineData("""{"success":true,"hostname":"translations.tightwiki.com"}""")]
        [InlineData("""{"success":true,"hostname":"evil.example.com","action":"verify"}""")]
        [InlineData("""{"success":true,"action":"verify"}""")]
        [InlineData("not json at all")]
        public async Task TurnstileRejectsAnythingThatIsNotExactlyRight(string json)
        {
            Assert.False(await CreateVerifier(new FakeCloudflare(json)).VerifyAsync("token", null));
        }

        [Theory]
        [InlineData("""{"success":false,"error-codes":["timeout-or-duplicate"]}""", TurnstileFailure.Rejected)]
        [InlineData("""{"success":true,"hostname":"translations.tightwiki.com","action":"signup"}""", TurnstileFailure.WrongAction)]
        [InlineData("""{"success":true,"hostname":"translate.tightwiki.com","action":"verify"}""", TurnstileFailure.WrongHost)]
        public async Task TurnstileSaysWhyItRefusedAToken(string json, TurnstileFailure expected)
        {
            var verdict = await CreateVerifier(new FakeCloudflare(json)).VerifyDetailedAsync("token", null);

            Assert.False(verdict.Success);
            Assert.Equal(expected, verdict.Failure);
            Assert.Equal(expected is TurnstileFailure.WrongAction or TurnstileFailure.WrongHost, verdict.IsSetupProblem);
        }

        [Fact]
        public async Task TurnstileSaysWhyItCouldNotCheckAtAll()
        {
            var good = """{"success":true,"hostname":"translations.tightwiki.com","action":"verify"}""";

            Assert.Equal(TurnstileFailure.Unavailable, (await CreateVerifier(new FakeCloudflare(good, HttpStatusCode.BadGateway)).VerifyDetailedAsync("t", null)).Failure);
            Assert.Equal(TurnstileFailure.NoToken, (await CreateVerifier(new FakeCloudflare(good)).VerifyDetailedAsync("", null)).Failure);

            var notConfigured = await CreateVerifier(new FakeCloudflare(good), secret: "").VerifyDetailedAsync("t", null);
            Assert.Equal(TurnstileFailure.NotConfigured, notConfigured.Failure);
            Assert.True(notConfigured.IsSetupProblem);

            Assert.True((await CreateVerifier(new FakeCloudflare(good)).VerifyDetailedAsync("t", null)).Success);
        }

        [Fact]
        public async Task TurnstileFailsClosedWhenCloudflareErrorsOrTheTokenIsMissing()
        {
            var good = """{"success":true,"hostname":"translations.tightwiki.com","action":"verify"}""";

            Assert.False(await CreateVerifier(new FakeCloudflare(good, HttpStatusCode.InternalServerError)).VerifyAsync("token", null));

            var handler = new FakeCloudflare(good);
            var verifier = CreateVerifier(handler);
            Assert.False(await verifier.VerifyAsync(null, null));
            Assert.False(await verifier.VerifyAsync("", null));
            Assert.False(await verifier.VerifyAsync(new string('a', 2049), null));
            Assert.Equal(0, handler.Calls); //nothing is sent to Cloudflare for tokens that cannot be real
        }

        [Fact]
        public async Task TurnstileRefusesEverythingUntilItIsConfigured()
        {
            var good = """{"success":true,"hostname":"translations.tightwiki.com","action":"verify"}""";

            var noSecret = CreateVerifier(new FakeCloudflare(good), secret: "");
            Assert.False(noSecret.IsConfigured);
            Assert.False(await noSecret.VerifyAsync("token", null));

            var noHosts = CreateVerifier(new FakeCloudflare(good), hostnames: []);
            Assert.False(noHosts.IsConfigured);
            Assert.False(await noHosts.VerifyAsync("token", null));
        }

        #endregion

        #region Visitors.

        [Fact]
        public void VisitorsAreOnlyVerifiedWhenTheyHaveTheCookie()
        {
            Assert.False(Visitor.From(null).IsVerified);
            Assert.False(Visitor.From(new System.Security.Claims.ClaimsPrincipal()).IsVerified);

            var member = Visitor.From(Visitor.CreatePrincipal("abc", isAdmin: false));
            Assert.True(member.IsVerified);
            Assert.False(member.IsAdmin);
            Assert.Equal("abc", member.Id);

            Assert.True(Visitor.From(Visitor.CreatePrincipal("abc", isAdmin: true)).IsAdmin);
            Assert.Equal(string.Empty, Visitor.Anonymous.QueryId);
        }

        [Theory]
        [InlineData(10, "just now")]
        [InlineData(90, "a minute ago")]
        [InlineData(600, "10 minutes ago")]
        [InlineData(5400, "an hour ago")]
        [InlineData(3 * 3600, "3 hours ago")]
        [InlineData(30 * 3600, "yesterday")]
        [InlineData(5 * 86400, "5 days ago")]
        public void TimeAgoIsReadable(int secondsAgo, string expected)
        {
            var now = new DateTime(2026, 10, 13, 12, 0, 0, DateTimeKind.Utc);

            Assert.Equal(expected, TimeAgo.Format(now.AddSeconds(-secondsAgo), now));
        }

        #endregion
    }
}
