using Microsoft.Extensions.Options;
using TightWiki.TranslationSite.Services;

namespace TightWiki.Tests.Unit
{
    /// <summary>
    /// Working out which languages a visitor reads (from their browser, or what they chose), and limiting what they see to them.
    /// </summary>
    public class TranslationSiteLanguageTests : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "TwLanguageTests_" + Guid.NewGuid().ToString("N"));

        private static readonly Lazy<TranslationCatalog> _catalog = new(() => new TranslationCatalog(TranslationSiteTests.FindTranslations()));

        private static TranslationCatalog Catalog => _catalog.Value;

        public TranslationSiteLanguageTests()
        {
            Directory.CreateDirectory(_directory);
        }

        public void Dispose()
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            try { Directory.Delete(_directory, true); } catch { /* A temporary folder is not worth failing a test over. */ }
            GC.SuppressFinalize(this);
        }

        private SuggestionService CreateService()
            => new(Catalog, Options.Create(new SiteOptions { SuggestionsPath = Path.Combine(_directory, Guid.NewGuid().ToString("N") + ".db") }));

        #region Finding a language from what a browser sends.

        [Theory]
        [InlineData("de", "German")]
        [InlineData("de-DE", "German")]
        [InlineData("DE-at", "German")]
        [InlineData("pt-BR", "Portuguese")]
        [InlineData("fr_CA", "French")]
        [InlineData("zh-CN", "Chinese simplified")]
        [InlineData("zh-SG", "Chinese simplified")]
        [InlineData("zh", "Chinese simplified")]
        [InlineData("zh-Hans", "Chinese simplified")]
        [InlineData("zh-Hans-CN", "Chinese simplified")]
        [InlineData("zh-TW", "Chinese traditional")]
        [InlineData("zh-HK", "Chinese traditional")]
        [InlineData("zh-Hant", "Chinese traditional")]
        [InlineData("zh-Hant-TW", "Chinese traditional")]
        [InlineData("no", "Bokmål")]
        [InlineData("nb-NO", "Bokmål")]
        [InlineData("nn", "Norwegian")]
        [InlineData("he", "Hebrew")]
        [InlineData("ar-EG", "Arabic")]
        public void ALanguageTagFindsTheLanguage(string tag, string expected)
        {
            Assert.Equal(expected, Catalog.FindLanguageByTag(tag)?.Name);
        }

        [Theory]
        [InlineData("en")]
        [InlineData("en-US")]
        [InlineData("xx")]
        [InlineData("*")]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void EnglishAndUnknownTagsFindNothing(string? tag)
        {
            Assert.Null(Catalog.FindLanguageByTag(tag));
        }

        #endregion

        #region What the browser asks for.

        [Theory]
        [InlineData("de-DE,de;q=0.9,en-US;q=0.8,en;q=0.7", "de")]
        [InlineData("fr-CH, fr;q=0.9, de;q=0.8, en;q=0.7", "fr,de")]
        [InlineData("en;q=0.9,es;q=1.0,pt-BR;q=0.5", "es,pt")]               //most wanted first, whatever the order
        [InlineData("de;q=0, fr", "fr")]                                      //q=0 means "not acceptable"
        [InlineData("de,fr,es,it,pt", "de,fr,es")]                            //only the first few
        [InlineData("zh-TW,zh;q=0.9", "zh-Hant,zh-Hans")]
        [InlineData("nb-NO,no;q=0.9,nn;q=0.8", "nb,nn")]                      //nb and no are the same language here
        [InlineData("ja,en-US;q=0.9", "ja")]
        [InlineData("de, *;q=0.5", "de")]
        public void TheBrowsersLanguagesAreUnderstood(string header, string expected)
        {
            Assert.Equal(expected.Split(','), LanguagePreferences.ParseAcceptLanguage(Catalog, header));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("en")]
        [InlineData("en-US,en;q=0.9")]            //English only: there is nothing to pick
        [InlineData("*")]
        [InlineData("not a language header at all")]
        [InlineData("de;q=abc;;;,;,")]
        public void AnEnglishOnlyOrUnusableHeaderPicksNothing(string? header)
        {
            var found = LanguagePreferences.ParseAcceptLanguage(Catalog, header);

            //(the odd one has "de" in it, which is a language, so only check that nothing it finds is English)
            Assert.DoesNotContain("en", found);
            if (header is null or "" or "en" or "en-US,en;q=0.9" or "*" or "not a language header at all")
            {
                Assert.Empty(found);
            }
        }

        #endregion

        #region What was chosen.

        [Theory]
        [InlineData("de,fr", "de,fr")]
        [InlineData("de, fr ,es", "de,fr,es")]
        [InlineData("xx,de", "de")]                                            //unknown ones are dropped
        [InlineData("de,DE,de", "de")]                                         //and so are repeats
        [InlineData("zh-Hans,ja", "zh-Hans,ja")]
        [InlineData("en,de", "de")]                                            //English is not something to choose
        [InlineData("de,fr,es,it,pt,ja,ko,ru", "de,fr,es,it,pt,ja")]           //no more than six
        public void TheChosenLanguagesAreReadFromTheCookie(string cookie, string expected)
        {
            Assert.Equal(expected.Split(','), LanguagePreferences.ParseCookie(Catalog, cookie));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(",,,")]
        [InlineData("-")]                         //"show all languages"
        [InlineData("<script>alert(1)</script>")]
        [InlineData("'; DROP TABLE Suggestion; --")]
        [InlineData("../../etc/passwd")]
        public void ACookieThatIsEmptyOrNonsenseChoosesNothing(string? cookie)
        {
            Assert.Empty(LanguagePreferences.ParseCookie(Catalog, cookie));
        }

        #endregion

        #region Searching and finding gaps in some languages.

        private static PhraseEntry PhraseWithGermanAndFrench()
            => Catalog.Phrases.First(o => o.Translations.ContainsKey("German") && o.Translations.ContainsKey("French")
                && !o.English.Contains('{') && o.Translations["German"].Length > 8
                && !Catalog.Search(o.Translations["German"], "French", 0, 1).Items.Any());

        [Fact]
        public void ASearchCanBeLimitedToSeveralLanguages()
        {
            var phrase = PhraseWithGermanAndFrench();
            var german = phrase.Translations["German"];

            //Found by its German text when German is among the languages, and not when it is not.
            Assert.Contains(Catalog.SearchIn(german, ["German", "French"], 0, 50).Items, o => o.Phrase == phrase);
            Assert.Contains(Catalog.SearchIn(german, ["German"], 0, 50).Items, o => o.Phrase == phrase);
            Assert.DoesNotContain(Catalog.SearchIn(german, ["French", "Spanish"], 0, 50).Items, o => o.Phrase == phrase);
        }

        [Fact]
        public void AnEmptyOrUnknownSetOfLanguagesMeansEveryLanguage()
        {
            var german = PhraseWithGermanAndFrench().Translations["German"];

            Assert.Equal(Catalog.SearchIn(german, null, 0, 50).Total, Catalog.SearchIn(german, [], 0, 50).Total);
            Assert.Equal(Catalog.SearchIn(german, null, 0, 50).Total, Catalog.SearchIn(german, ["Klingon"], 0, 50).Total);
            Assert.Equal(Catalog.Search(german, null, 0, 50).Total, Catalog.SearchIn(german, null, 0, 50).Total);
        }

        [Fact]
        public void TheSingleLanguageSearchStillWorks()
        {
            var phrase = PhraseWithGermanAndFrench();

            Assert.Equal(
                Catalog.SearchIn(phrase.Translations["German"], ["German"], 0, 50).Total,
                Catalog.Search(phrase.Translations["German"], "German", 0, 50).Total);
        }

        [Fact]
        public void MissingTranslationsCanBeLimitedToSomeLanguages()
        {
            var gaps = Catalog.GetGapsIn(10000, ["German", "French"]);

            Assert.All(gaps, o => Assert.Contains(o.Language.Name, new[] { "German", "French" }));
            Assert.DoesNotContain(gaps, o => o.Phrase.Translations.ContainsKey(o.Language.Name));

            Assert.Equal(Catalog.GetGaps(10000).Count, Catalog.GetGapsIn(10000, null).Count);
        }

        #endregion

        #region Suggestions in some languages.

        private static (PhraseEntry A, PhraseEntry B, PhraseEntry C) ThreePhrases()
        {
            var list = Catalog.Phrases.Where(o => o.Translations.ContainsKey("German") && !o.English.Contains('{')).Take(3).ToList();
            return (list[0], list[1], list[2]);
        }

        [Fact]
        public void TheFeedCanBeLimitedToSomeLanguages()
        {
            var service = CreateService();
            var (a, b, c) = ThreePhrases();

            service.Submit("alice", a.English, "German", "Ein deutscher Vorschlag");
            service.Submit("alice", b.English, "French", "Une suggestion francaise");
            service.Submit("alice", c.English, "Spanish", "Una sugerencia espanola");

            Assert.Equal(3, service.GetRecent("anyone", 10).Count);
            Assert.Equal(3, service.GetRecent("anyone", 10, null).Count);
            Assert.Equal(3, service.GetRecent("anyone", 10, []).Count);

            var german = Assert.Single(service.GetRecent("anyone", 10, ["German"]));
            Assert.Equal("German", german.Language);

            Assert.Equal(["French", "German"], service.GetRecent("anyone", 10, ["German", "French"]).Select(o => o.Language).Order().ToArray());
            Assert.Empty(service.GetRecent("anyone", 10, ["Hebrew"]));
        }

        [Fact]
        public void VoteNowCanBeLimitedToSomeLanguages()
        {
            var service = CreateService();
            var (a, b, _) = ThreePhrases();

            service.Submit("alice", a.English, "German", "Ein deutscher Vorschlag");
            service.Submit("alice", b.English, "French", "Une suggestion francaise");

            var offered = service.GetNeedingVotes("bob", 10, ["French"]);

            Assert.Equal("French", Assert.Single(offered).Language);
            Assert.Equal(2, service.GetNeedingVotes("bob", 10).Count);
        }

        [Fact]
        public void SearchResultsShowOnlySuggestionsInTheChosenLanguages()
        {
            var service = CreateService();
            var (a, _, _) = ThreePhrases();

            service.Submit("alice", a.English, "German", "Ein deutscher Vorschlag");
            service.Submit("alice", a.English, "French", "Une suggestion francaise");
            service.Submit("alice", a.English, "Spanish", "Una sugerencia espanola");

            var phrase = new[] { a.English };

            Assert.Equal(3, service.GetForPhrasesIn("bob", phrase, null).Count);
            Assert.Equal(2, service.GetForPhrasesIn("bob", phrase, ["German", "Spanish"]).Count);
            Assert.Equal("French", Assert.Single(service.GetForPhrasesIn("bob", phrase, ["French"])).Language);

            //The version that takes one language still behaves as it did.
            Assert.Equal("German", Assert.Single(service.GetForPhrases("bob", phrase, "German")).Language);
            Assert.Equal(3, service.GetForPhrases("bob", phrase, null).Count);
        }

        #endregion
    }
}
