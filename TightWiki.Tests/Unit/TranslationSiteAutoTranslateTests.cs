using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TightWiki.TranslationSite.Services;

namespace TightWiki.Tests.Unit
{
    /// <summary>
    /// The administrator's auto-translate button on the translations site.
    /// </summary>
    public class TranslationSiteAutoTranslateTests : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "TwAutoTranslateTests_" + Guid.NewGuid().ToString("N"));

        private static readonly Visitor Admin = new("admin-1", IsVerified: true, IsAdmin: true);
        private static readonly Visitor Regular = new("visitor-1", IsVerified: true, IsAdmin: false);

        private static readonly Lazy<TranslationCatalog> _catalog = new(() => new TranslationCatalog(TranslationSiteTests.FindTranslations()));

        private sealed class ManualTime : TimeProvider
        {
            private DateTimeOffset _now = new(2026, 10, 14, 9, 0, 0, TimeSpan.Zero);
            public override DateTimeOffset GetUtcNow() => _now;
            public void Advance(TimeSpan span) => _now += span;
        }

        private sealed class FakeModel : ITranslationModel
        {
            public bool IsAvailable { get; set; } = true;
            public Func<string, string, string?> Answer { get; set; } = (_, _) => "Eine neue Übersetzung";
            public Exception? Throws { get; set; }
            public int Calls { get; private set; }
            public string? LastLanguage { get; private set; }
            public string? LastEnglish { get; private set; }

            public Task<string?> TranslateAsync(string english, string languageName, CancellationToken cancellationToken = default)
            {
                Calls++;
                LastEnglish = english;
                LastLanguage = languageName;

                if (Throws != null)
                {
                    throw Throws;
                }

                return Task.FromResult(Answer(english, languageName));
            }
        }

        public TranslationSiteAutoTranslateTests()
        {
            Directory.CreateDirectory(_directory);
        }

        public void Dispose()
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            try { Directory.Delete(_directory, true); } catch { /* A temporary folder is not worth failing a test over. */ }
            GC.SuppressFinalize(this);
        }

        private (AutoTranslationService Auto, SuggestionService Suggestions, FakeModel Model) Create(
            ManualTime? time = null, Action<EinkrOptions>? configure = null)
        {
            time ??= new ManualTime();
            var suggestions = new SuggestionService(_catalog.Value,
                Options.Create(new SiteOptions { SuggestionsPath = Path.Combine(_directory, Guid.NewGuid().ToString("N") + ".db") }), time);

            var einkr = new EinkrOptions();
            configure?.Invoke(einkr);

            var model = new FakeModel();
            var auto = new AutoTranslationService(_catalog.Value, suggestions, model, Options.Create(einkr), NullLogger<AutoTranslationService>.Instance, time);

            return (auto, suggestions, model);
        }

        private static PhraseEntry GermanPhrase()
            => _catalog.Value.Phrases.First(o => o.Translations.ContainsKey("German") && !o.English.Contains('{') && o.English.Length < 40);

        #region Reading the model's answer.

        [Theory]
        [InlineData("""{"translation": "Hallo Welt"}""", "Hallo Welt")]
        [InlineData("""  {"translation":"  Hallo Welt  "}  """, "Hallo Welt")]
        [InlineData("""{"translation": "Hallo {0}"}""", "Hallo {0}")]
        [InlineData("""{"translation": "Sagt \"Hallo\" zu <strong>{0}</strong> & Co"}""", "Sagt \"Hallo\" zu <strong>{0}</strong> & Co")]
        [InlineData("""{"translation": "Zürich ✓ שלום"}""", "Zürich ✓ שלום")]
        [InlineData("""{"translation": "Hallo", "note": "extra properties are ignored"}""", "Hallo")]
        public void TheAnswerIsReadFromTheJson(string response, string expected)
        {
            Assert.Equal(expected, EinkrTranslationModel.ParseResponse(response));
        }

        [Theory]
        [InlineData("""
            ```json
            {"translation": "Hallo Welt"}
            ```
            """, "Hallo Welt")] //a code fence around it
        [InlineData("""
            ```
            {"translation": "Hallo Welt"}
            ```
            """, "Hallo Welt")]
        [InlineData("""Here you go: {"translation": "Hallo Welt"} Hope that helps!""", "Hallo Welt")] //chatter around it
        public void ModelsThatWrapTheJsonAreStillUnderstood(string response, string expected)
        {
            Assert.Equal(expected, EinkrTranslationModel.ParseResponse(response));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("Hallo Welt")] //not JSON
        [InlineData("Hallo {0}")] //a placeholder is not a JSON object
        [InlineData("""{"translation": ""}""")]
        [InlineData("""{"translation": "   "}""")]
        [InlineData("""{"translation": "never closed""")]
        [InlineData("""{"text": "wrong property"}""")]
        [InlineData("""{"translation": 42}""")]
        [InlineData("""{"translation": ["a", "b"]}""")]
        [InlineData("""["Hallo Welt"]""")]
        [InlineData("<Phrase_0>Hallo Welt</Phrase_0>")] //the answer of the other prompt, which this one no longer uses
        public void ABrokenAnswerIsNotUsed(string? response)
        {
            Assert.Null(EinkrTranslationModel.ParseResponse(response));
        }

        [Fact]
        public void ThePromptIsForOnePhraseAndAsksForJson()
        {
            var prompt = File.ReadAllText(FindPrompt());

            Assert.Contains("{sourceLanguage}", prompt);
            Assert.Contains("{targetLanguage}", prompt);
            Assert.Contains("{\"translation\"", prompt);
            Assert.Contains("{\"phrase\"", prompt);
            Assert.DoesNotContain("Phrase_0", prompt); //LocalizerScan's batch format, which is not used here
            Assert.DoesNotContain("{{0}}", prompt);    //placeholders are written as they appear in the phrases
            Assert.Contains("{0}", prompt);
        }

        [Fact]
        public void TheServerIsToldTheExactShapeOfTheAnswer()
        {
            using var schema = System.Text.Json.JsonDocument.Parse(EinkrTranslationModel.ResponseSchema);
            var root = schema.RootElement;

            Assert.Equal("object", root.GetProperty("type").GetString());
            Assert.Equal("string", root.GetProperty("properties").GetProperty("translation").GetProperty("type").GetString());
            Assert.Equal(["translation"], root.GetProperty("required").EnumerateArray().Select(o => o.GetString()!).ToArray());
            Assert.False(root.GetProperty("additionalProperties").GetBoolean()); //nothing but the translation

            //And an answer of that shape is read.
            Assert.Equal("Hallo", EinkrTranslationModel.ParseResponse("""{"translation":"Hallo"}"""));
        }

        private static string FindPrompt()
        {
            for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
            {
                var candidate = Path.Combine(directory.FullName, "TightWiki.TranslationSite", "Prompts", "TranslationPrompt.txt");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            throw new FileNotFoundException("Could not find TranslationPrompt.txt above the test folder.");
        }

        [Fact]
        public async Task TheModelIsOffAndSilentWithoutAnApiKey()
        {
            using var model = new EinkrTranslationModel(Options.Create(new EinkrOptions { ApiKey = "" }));

            Assert.False(model.IsAvailable);
            Assert.Null(await model.TranslateAsync("Hello", "German"));
        }

        [Fact]
        public void TheModelIsAvailableOnceItHasAnApiKey()
        {
            using var model = new EinkrTranslationModel(Options.Create(new EinkrOptions { ApiKey = "vrc_not_a_real_key" }));

            Assert.True(model.IsAvailable);
        }

        #endregion

        #region Who can use it.

        [Fact]
        public async Task OnlyAdministratorsCanAutoTranslate()
        {
            var (auto, suggestions, model) = Create();
            var phrase = GermanPhrase();

            foreach (var requester in new[] { Regular, Visitor.Anonymous, new Visitor(null, false, true) })
            {
                var result = await auto.TranslateAsync(requester, phrase.English, "German");

                Assert.False(result.Added);
                Assert.Contains("administrators", result.Message);
            }

            Assert.Equal(0, model.Calls); //the model is not even asked
            Assert.Empty(suggestions.GetRecent("anyone", 10));
        }

        [Fact]
        public async Task NothingHappensWhenTheModelIsNotSetUp()
        {
            var (auto, suggestions, model) = Create();
            model.IsAvailable = false;

            var result = await auto.TranslateAsync(Admin, GermanPhrase().English, "German");

            Assert.False(auto.IsAvailable);
            Assert.False(result.Added);
            Assert.Contains("not been set up", result.Message);
            Assert.Equal(0, model.Calls);
            Assert.Empty(suggestions.GetRecent("anyone", 10));
        }

        [Fact]
        public async Task AnUnknownPhraseOrLanguageIsRefused()
        {
            var (auto, _, model) = Create();

            Assert.False((await auto.TranslateAsync(Admin, "This phrase is not in the translations", "German")).Added);
            Assert.False((await auto.TranslateAsync(Admin, GermanPhrase().English, "Klingon")).Added);
            Assert.Equal(0, model.Calls);
        }

        #endregion

        #region What gets added.

        [Fact]
        public async Task ATranslationIsAddedAsASuggestionForThatLanguage()
        {
            var (auto, suggestions, model) = Create();
            var phrase = GermanPhrase();

            var result = await auto.TranslateAsync(Admin, phrase.English, "German");

            Assert.True(result.Added, result.Message);
            Assert.Equal(phrase.English, model.LastEnglish);
            Assert.Equal("German", model.LastLanguage);

            var added = Assert.Single(suggestions.GetRecent("anyone", 10));
            Assert.Equal("Eine neue Übersetzung", added.Text);
            Assert.Equal("German", added.Language);
            Assert.Equal(phrase.English, added.English);
            Assert.Equal(SuggestionService.AutoSubmitterKey, added.SubmitterKey);
        }

        [Fact]
        public async Task AMachineTranslationStartsWithNoVotesAndCanBeVotedOnByEveryone()
        {
            var (auto, suggestions, _) = Create();
            var id = (await auto.TranslateAsync(Admin, GermanPhrase().English, "German")).Suggestion!.Id;

            Assert.Equal(0, suggestions.GetById("anyone", id)!.Score);

            //Including the administrator who asked for it, because it is not "theirs".
            Assert.True(suggestions.Vote(Admin.Id!, id, 1).Ok);
            Assert.True(suggestions.Vote("someone-else", id, 1).Ok);
            Assert.Equal(2, suggestions.GetById("anyone", id)!.Score);

            //And it is offered to people who are looking for something to vote on.
            var another = CreateOfferedTo(suggestions, "a-new-visitor");
            Assert.Contains(another, o => o.Id == id);
        }

        private static List<Suggestion> CreateOfferedTo(SuggestionService suggestions, string visitor)
            => suggestions.GetNeedingVotes(visitor, 10);

        [Fact]
        public async Task NothingIsAddedWhenTheTranslationIsTheSameAsTheCurrentOne()
        {
            var (auto, suggestions, model) = Create();
            var phrase = GermanPhrase();
            model.Answer = (_, _) => phrase.Translations["German"];

            var result = await auto.TranslateAsync(Admin, phrase.English, "German");

            Assert.False(result.Added);
            Assert.Contains("same as the current", result.Message);
            Assert.Empty(suggestions.GetRecent("anyone", 10));
        }

        [Fact]
        public async Task TheSameTranslationWithDifferentCapitalsIsStillTheSame()
        {
            var (auto, suggestions, model) = Create();
            var phrase = GermanPhrase();
            model.Answer = (_, _) => phrase.Translations["German"].ToUpperInvariant();

            Assert.False((await auto.TranslateAsync(Admin, phrase.English, "German")).Added);
            Assert.Empty(suggestions.GetRecent("anyone", 10));
        }

        [Fact]
        public async Task NothingIsAddedWhenItHasAlreadyBeenSuggestedAndNoVoteIsCast()
        {
            var (auto, suggestions, model) = Create();
            var phrase = GermanPhrase();

            var theirs = suggestions.Submit("alice", phrase.English, "German", "Eine neue Übersetzung").Suggestion!;
            var before = suggestions.GetById("anyone", theirs.Id)!;

            var result = await auto.TranslateAsync(Admin, phrase.English, "German");

            Assert.False(result.Added);
            Assert.Contains("already been suggested", result.Message);
            Assert.Single(suggestions.GetRecent("anyone", 10));

            var after = suggestions.GetById("anyone", theirs.Id)!;
            Assert.Equal(before.UpVotes, after.UpVotes);
            Assert.Equal(before.DownVotes, after.DownVotes);
            Assert.Equal(1, model.Calls);
        }

        [Fact]
        public async Task AMachineTranslationIsCheckedLikeAnyOtherSuggestion()
        {
            var (auto, suggestions, model) = Create();
            var phrase = GermanPhrase();

            model.Answer = (_, _) => "Сиротские вложения"; //the wrong alphabet for German
            var wrongAlphabet = await auto.TranslateAsync(Admin, phrase.English, "German");
            Assert.False(wrongAlphabet.Added);
            Assert.Contains("Cyrillic", wrongAlphabet.Message);

            model.Answer = (_, _) => "Zwei\r\nZeilen";
            Assert.False((await auto.TranslateAsync(Admin, phrase.English, "German")).Added);

            var withPlaceholder = _catalog.Value.Phrases.First(o => o.English.Contains("{0}") && o.Translations.ContainsKey("German"));
            model.Answer = (_, _) => "Ohne Platzhalter";
            var broken = await auto.TranslateAsync(Admin, withPlaceholder.English, "German");
            Assert.False(broken.Added);
            Assert.Contains("placeholders", broken.Message);

            Assert.Empty(suggestions.GetRecent("anyone", 10));
        }

        [Fact]
        public async Task ABrokenOrMissingAnswerAddsNothing()
        {
            var (auto, suggestions, model) = Create();

            model.Answer = (_, _) => null;
            var nothing = await auto.TranslateAsync(Admin, GermanPhrase().English, "German");
            Assert.False(nothing.Added);
            Assert.Contains("nothing usable", nothing.Message);

            model.Answer = (_, _) => "   ";
            Assert.False((await auto.TranslateAsync(Admin, GermanPhrase().English, "German")).Added);

            Assert.Empty(suggestions.GetRecent("anyone", 10));
        }

        [Fact]
        public async Task AFailureInTheServiceIsReportedWithoutItsDetails()
        {
            var (auto, suggestions, model) = Create();
            model.Throws = new HttpRequestException("401 Unauthorized for key vrc_super_secret_key");

            var result = await auto.TranslateAsync(Admin, GermanPhrase().English, "German");

            Assert.False(result.Added);
            Assert.Contains("did not respond", result.Message);
            Assert.DoesNotContain("vrc_super_secret_key", result.Message); //what the service said stays in the log
            Assert.Empty(suggestions.GetRecent("anyone", 10));
        }

        [Fact]
        public async Task ASuggestionIsAnnouncedSoOpenPagesRefresh()
        {
            var (auto, suggestions, _) = Create();
            var changes = 0;
            suggestions.Changed += () => changes++;

            await auto.TranslateAsync(Admin, GermanPhrase().English, "German");

            Assert.Equal(1, changes);
        }

        [Fact]
        public async Task ItCanBeUsedForEveryLanguage()
        {
            var (auto, suggestions, model) = Create();
            var phrase = GermanPhrase();
            model.Answer = (_, language) => language switch
            {
                "French" => "Une nouvelle traduction",
                "Hebrew" => "תרגום חדש",
                _ => null
            };

            Assert.True((await auto.TranslateAsync(Admin, phrase.English, "French")).Added);
            Assert.True((await auto.TranslateAsync(Admin, phrase.English, "Hebrew")).Added);

            var added = suggestions.GetForPhrases("anyone", [phrase.English], null);
            Assert.Equal(["French", "Hebrew"], added.Select(o => o.Language).Order().ToArray());
        }

        #endregion

        #region Limits.

        [Fact]
        public async Task AutoTranslatingIsRateLimitedPerAdministrator()
        {
            var time = new ManualTime();
            var (auto, _, model) = Create(time, o => o.AutoTranslatesPerMinute = 2);
            var phrases = _catalog.Value.Phrases.Where(o => o.Translations.ContainsKey("German") && !o.English.Contains('{')).Take(4).ToList();

            Assert.True((await auto.TranslateAsync(Admin, phrases[0].English, "German")).Added);
            model.Answer = (_, _) => "Eine andere Übersetzung";
            Assert.True((await auto.TranslateAsync(Admin, phrases[1].English, "German")).Added);

            var third = await auto.TranslateAsync(Admin, phrases[2].English, "German");
            Assert.False(third.Added);
            Assert.Contains("very quickly", third.Message);
            Assert.Equal(2, model.Calls); //it did not reach the model, which is the point

            time.Advance(TimeSpan.FromSeconds(61));
            model.Answer = (_, _) => "Noch eine Übersetzung";
            Assert.True((await auto.TranslateAsync(Admin, phrases[2].English, "German")).Added);
        }

        #endregion
    }
}
