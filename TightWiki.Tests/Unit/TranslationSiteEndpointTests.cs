using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;
using TightWiki.TranslationSite.Services;

namespace TightWiki.Tests.Unit
{
    /// <summary>
    /// Runs the translations site in memory. Cloudflare is replaced with a stand-in, so these tests never
    ///  involve (or bypass) a real captcha: they check what the site does with the answer.
    /// </summary>
    public sealed class TranslationSiteFactory : WebApplicationFactory<Program>
    {
        private readonly int _gateAttemptsPerMinute;

        /// <summary>
        /// The tests make a lot of requests from one address, so the limit is raised unless the test is about the limit.
        /// (xUnit only allows a fixture one public constructor, which is why the other is internal.)
        /// </summary>
        public TranslationSiteFactory()
            : this(1000)
        {
        }

        internal TranslationSiteFactory(int gateAttemptsPerMinute)
        {
            _gateAttemptsPerMinute = gateAttemptsPerMinute;
        }

        private readonly string _directory = Path.Combine(Path.GetTempPath(), "TwTranslationSiteHost_" + Guid.NewGuid().ToString("N"));

        public const string AdminKey = "test-admin-key";

        public StubVerifier Verifier { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            Directory.CreateDirectory(_directory);

            builder.UseSetting("Site:TranslationsPath", TranslationSiteTests.FindTranslations());
            builder.UseSetting("Site:SuggestionsPath", Path.Combine(_directory, "Suggestions.db"));
            builder.UseSetting("Site:AdminKey", AdminKey);
            builder.UseSetting("Site:GateAttemptsPerMinute", _gateAttemptsPerMinute.ToString());
            builder.UseSetting("Einkr:ApiKey", "vrc_pretend_key_for_tests");
            builder.UseSetting("Turnstile:SiteKey", "site-key-for-tests");
            builder.UseSetting("Turnstile:SecretKey", "secret-for-tests");

            builder.ConfigureTestServices(services => services.AddTransient<TurnstileVerifier>(_ => Verifier));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                try { Directory.Delete(_directory, true); } catch { /* A temporary folder is not worth failing a test over. */ }
            }
        }

        public sealed class StubVerifier()
            : TurnstileVerifier(new HttpClient(), Options.Create(new TurnstileOptions { SiteKey = "site-key-for-tests" }), NullLogger<TurnstileVerifier>.Instance)
        {
            public bool Result { get; set; }

            /// <summary>
            /// What a failed check is reported as.
            /// </summary>
            public TurnstileFailure FailureWhenRejected { get; set; } = TurnstileFailure.Rejected;

            public string? LastToken { get; private set; }

            public override bool IsConfigured => true;

            public override Task<TurnstileVerdict> VerifyDetailedAsync(string? token, string? remoteIp, CancellationToken cancellationToken = default)
            {
                LastToken = token;
                return Task.FromResult(Result ? new TurnstileVerdict() : new TurnstileVerdict(FailureWhenRejected));
            }
        }
    }

    public class TranslationSiteEndpointTests(TranslationSiteFactory factory) : IClassFixture<TranslationSiteFactory>
    {
        private HttpClient CreateClient()
            => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false });

        private static string? CookieFrom(HttpResponseMessage response)
            => response.Headers.TryGetValues("Set-Cookie", out var values)
                ? values.FirstOrDefault(o => o.StartsWith("tw_translations=", StringComparison.Ordinal))?.Split(';')[0]
                : null;

        private static HttpRequestMessage Get(string url, string? cookie = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (cookie != null)
            {
                request.Headers.Add("Cookie", cookie);
            }

            return request;
        }

        private static async Task<HttpResponseMessage> Redirected(HttpResponseMessage response)
        {
            if (response.StatusCode != HttpStatusCode.Redirect)
            {
                Assert.Fail($"Expected a redirect but got {(int)response.StatusCode}: {(await response.Content.ReadAsStringAsync())[..Math.Min(1500, (await response.Content.ReadAsStringAsync()).Length)]}");
            }

            return response;
        }

        private static FormUrlEncodedContent Form(params (string Key, string Value)[] fields)
            => new(fields.Select(o => new KeyValuePair<string, string>(o.Key, o.Value)));

        [Fact]
        public async Task PagesAreServed()
        {
            using var client = CreateClient();

            var home = await client.GetAsync("/");
            var homeText = await home.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, home.StatusCode);
            Assert.Contains("speak your language", homeText);

            var search = await client.GetAsync("/search?q=backup");
            Assert.Equal(HttpStatusCode.OK, search.StatusCode);
            Assert.Contains("Database Backups", await search.Content.ReadAsStringAsync());

            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/admin")).StatusCode);
        }

        [Fact]
        public async Task TheCaptchaPageShowsOurWidgetForOurAction()
        {
            using var client = CreateClient();

            var text = await (await client.GetAsync("/gate?returnUrl=%2Fsearch")).Content.ReadAsStringAsync();

            Assert.Contains("data-sitekey=\"site-key-for-tests\"", text);
            Assert.Contains("data-action=\"verify\"", text);
            Assert.Contains("name=\"returnUrl\" value=\"/search\"", text);
            Assert.Contains("action=\"/gate/verify\"", text);
            Assert.Contains("data-error-callback=\"twGateError\"", text); //so that a widget error says why
            Assert.Contains("id=\"gateWidgetError\"", text);
            Assert.DoesNotContain("secret-for-tests", text); //the secret key must never reach the browser
        }

        [Fact]
        public async Task AFailedCaptchaGivesNoCookie()
        {
            factory.Verifier.Result = false;
            factory.Verifier.FailureWhenRejected = TurnstileFailure.Rejected;
            using var client = CreateClient();

            var response = await Redirected(await client.PostAsync("/gate/verify", Form(("cf-turnstile-response", "bad-token"), ("returnUrl", "/search"))));

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.StartsWith("/gate?error=1", response.Headers.Location!.OriginalString);
            Assert.Null(CookieFrom(response));
            Assert.Equal("bad-token", factory.Verifier.LastToken);
        }

        [Fact]
        public async Task APassedCaptchaGivesAVisitorCookieAndReturnsThemToThePage()
        {
            factory.Verifier.Result = true;
            using var client = CreateClient();

            var response = await client.PostAsync("/gate/verify", Form(("cf-turnstile-response", "good-token"), ("returnUrl", "/search?q=backup")));

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/search?q=backup", response.Headers.Location!.OriginalString);

            var cookie = CookieFrom(response);
            Assert.NotNull(cookie);
            Assert.Contains("httponly", response.Headers.GetValues("Set-Cookie").First(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task AVisitorKeepsTheirIdentityWhenTheyPassTheCaptchaAgain()
        {
            factory.Verifier.Result = true;
            using var client = CreateClient();

            var first = CookieFrom(await client.PostAsync("/gate/verify", Form(("cf-turnstile-response", "t"), ("returnUrl", "/"))))!;

            var request = new HttpRequestMessage(HttpMethod.Post, "/gate/verify") { Content = Form(("cf-turnstile-response", "t"), ("returnUrl", "/")) };
            request.Headers.Add("Cookie", first);
            var second = await client.SendAsync(request);

            //The identifier is inside the protected cookie, so it can only be compared by what it does:
            //  they are still signed in, and still a visitor rather than an administrator.
            Assert.NotNull(CookieFrom(second));
            var admin = await client.SendAsync(Get("/admin/export.csv", CookieFrom(second)));
            Assert.Equal(HttpStatusCode.Forbidden, admin.StatusCode);
        }

        [Theory]
        [InlineData("//evil.example.com/path")]
        [InlineData("https://evil.example.com/")]
        [InlineData("/\\evil.example.com")]
        [InlineData("javascript:alert(1)")]
        [InlineData("")]
        public async Task TheCaptchaCannotBeUsedToSendPeopleToAnotherSite(string returnUrl)
        {
            factory.Verifier.Result = true;
            using var client = CreateClient();

            var response = await client.PostAsync("/gate/verify", Form(("cf-turnstile-response", "t"), ("returnUrl", returnUrl)));

            Assert.Equal("/", response.Headers.Location!.OriginalString);
        }

        [Fact]
        public async Task TheAdminExportIsOnlyForAdministrators()
        {
            factory.Verifier.Result = true;
            using var client = CreateClient();

            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/admin/export.csv")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/admin/export.json")).StatusCode);

            //Passing the captcha makes someone a visitor, which is not enough.
            var visitorCookie = CookieFrom(await client.PostAsync("/gate/verify", Form(("cf-turnstile-response", "t"), ("returnUrl", "/"))))!;
            Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(Get("/admin/export.csv", visitorCookie))).StatusCode);
        }

        [Fact]
        public async Task GuessingAtTheCaptchaAndTheAdminKeyIsSlowedDown()
        {
            using var limited = new TranslationSiteFactory(3);
            using var client = limited.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            for (int attempt = 0; attempt < 3; attempt++)
            {
                var allowed = await client.PostAsync("/admin/login", Form(("key", "guess " + attempt)));
                Assert.Equal(HttpStatusCode.Redirect, allowed.StatusCode);
            }

            var blocked = await client.PostAsync("/admin/login", Form(("key", TranslationSiteFactory.AdminKey))); //even the right key
            Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
            Assert.Null(CookieFrom(blocked));

            //The captcha shares the same limit.
            Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsync("/gate/verify", Form(("cf-turnstile-response", "t")))).StatusCode);
        }

        [Fact]
        public async Task ASiteSetupProblemIsReportedDifferentlyFromAFailedCheck()
        {
            factory.Verifier.Result = false;
            using var client = CreateClient();

            foreach (var setupProblem in new[] { TurnstileFailure.WrongHost, TurnstileFailure.WrongAction, TurnstileFailure.NotConfigured })
            {
                factory.Verifier.FailureWhenRejected = setupProblem;
                var response = await Redirected(await client.PostAsync("/gate/verify", Form(("cf-turnstile-response", "t"), ("returnUrl", "/"))));
                Assert.StartsWith("/gate?error=setup", response.Headers.Location!.OriginalString);
            }

            foreach (var ordinary in new[] { TurnstileFailure.Rejected, TurnstileFailure.NoToken, TurnstileFailure.Unavailable })
            {
                factory.Verifier.FailureWhenRejected = ordinary;
                var response = await Redirected(await client.PostAsync("/gate/verify", Form(("cf-turnstile-response", "t"), ("returnUrl", "/"))));
                Assert.StartsWith("/gate?error=1", response.Headers.Location!.OriginalString);
            }
        }

        [Fact]
        public async Task TheCaptchaPageOnlyContinuesByItselfTheFirstTime()
        {
            using var client = CreateClient();

            var first = await (await client.GetAsync("/gate")).Content.ReadAsStringAsync();
            Assert.Contains("data-callback=\"twGateSolved\"", first);

            //After a failure it must wait for the visitor, or a check that always fails would repeat until it was rate limited.
            var afterFailure = await (await client.GetAsync("/gate?error=1")).Content.ReadAsStringAsync();
            Assert.DoesNotContain("data-callback", afterFailure);
            Assert.Contains("verify that", afterFailure);
            Assert.Contains("cf-turnstile", afterFailure);
        }

        [Fact]
        public async Task TheCaptchaPageDoesNotOfferToRetryWhenTheSiteIsSetUpWrongly()
        {
            using var client = CreateClient();

            var text = await (await client.GetAsync("/gate?error=setup")).Content.ReadAsStringAsync();

            Assert.Contains("trying again won", text);
            Assert.DoesNotContain("cf-turnstile", text);
            Assert.DoesNotContain("gateForm", text);
        }

        [Fact]
        public async Task BeingRateLimitedShowsAReadablePage()
        {
            using var limited = new TranslationSiteFactory(1);
            using var client = limited.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            await client.PostAsync("/admin/login", Form(("key", "guess")));
            var blocked = await client.PostAsync("/admin/login", Form(("key", "guess")));

            Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
            Assert.Equal("text/html", blocked.Content.Headers.ContentType?.MediaType);
            Assert.Contains("Too many attempts", await blocked.Content.ReadAsStringAsync());
            Assert.Equal("60", blocked.Headers.GetValues("Retry-After").Single());
        }

        [Fact]
        public async Task TheAutoTranslateButtonIsOnlyShownToAdministrators()
        {
            factory.Verifier.Result = true;
            using var client = CreateClient();

            //Anonymous.
            var anonymous = await (await client.GetAsync("/search?q=backup&lang=German")).Content.ReadAsStringAsync();
            Assert.Contains("Database Backups", anonymous);
            Assert.DoesNotContain("tw-auto-btn", anonymous);

            //Someone who has passed the captcha is still not an administrator.
            var visitorCookie = CookieFrom(await client.PostAsync("/gate/verify", Form(("cf-turnstile-response", "t"), ("returnUrl", "/"))))!;
            var visitor = await (await client.SendAsync(Get("/search?q=backup&lang=German", visitorCookie))).Content.ReadAsStringAsync();
            Assert.DoesNotContain("tw-auto-btn", visitor);

            //An administrator gets one beside each language.
            var adminCookie = CookieFrom(await client.PostAsync("/admin/login", Form(("key", TranslationSiteFactory.AdminKey))))!;
            var admin = await (await client.SendAsync(Get("/search?q=backup&lang=German", adminCookie))).Content.ReadAsStringAsync();
            Assert.Contains("tw-auto-btn", admin);
            Assert.Contains("aria-label=\"Auto-translate to German\"", admin);
        }

        /// <summary>
        /// Adds a German and a French suggestion (each with its own marker, so a page can be checked for them) and returns the markers.
        /// </summary>
        private (string German, string French) SeedSuggestions()
        {
            var suggestions = factory.Services.GetRequiredService<SuggestionService>();
            var catalog = factory.Services.GetRequiredService<TranslationCatalog>();

            var unique = Guid.NewGuid().ToString("N")[..8];
            var german = $"Vorschlag ZZDE{unique}";
            var french = $"Suggestion ZZFR{unique}";

            //Each is made against a different phrase so that the two can never be the same suggestion.
            var phrases = catalog.Phrases.Where(o => o.Translations.ContainsKey("German") && !o.English.Contains('{')).Skip(Random.Shared.Next(5, 200)).Take(2).ToList();

            Assert.True(suggestions.Submit("seed-" + unique, phrases[0].English, "German", german).Ok);
            Assert.True(suggestions.Submit("seed-" + unique, phrases[1].English, "French", french).Ok);

            return (german, french);
        }

        private async Task<string> HomeAsync(string? cookie = null, string? acceptLanguage = null, string url = "/")
        {
            using var client = CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            if (cookie != null)
            {
                request.Headers.Add("Cookie", cookie);
            }

            if (acceptLanguage != null)
            {
                request.Headers.Add("Accept-Language", acceptLanguage);
            }

            var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await response.Content.ReadAsStringAsync();
        }

        [Fact]
        public async Task TheHomePageOnlyShowsSuggestionsInTheLanguagesTheVisitorChose()
        {
            var (german, french) = SeedSuggestions();

            var page = await HomeAsync(cookie: "tw_languages=de");

            Assert.Contains(german, page);
            Assert.DoesNotContain(french, page);
            Assert.Contains("Your languages", page);
            Assert.DoesNotContain("picked from your browser", page); //they chose it, so it is not a guess
            Assert.Contains("Fresh suggestions in German", page);

            var both = await HomeAsync(cookie: "tw_languages=" + Uri.EscapeDataString("de,fr"));
            Assert.Contains(german, both);
            Assert.Contains(french, both);

            var frenchOnly = await HomeAsync(cookie: "tw_languages=fr");
            Assert.DoesNotContain(german, frenchOnly);
            Assert.Contains(french, frenchOnly);
        }

        [Fact]
        public async Task WithNoChoiceTheBrowsersLanguageIsUsedAndSaidSo()
        {
            var (german, french) = SeedSuggestions();

            var page = await HomeAsync(acceptLanguage: "de-DE,de;q=0.9,en-US;q=0.8,en;q=0.7");

            Assert.Contains(german, page);
            Assert.DoesNotContain(french, page);
            Assert.Contains("picked from your browser", page);
            Assert.Contains("Your languages", page);

            var other = await HomeAsync(acceptLanguage: "fr-FR,fr;q=0.9");
            Assert.Contains(french, other);
            Assert.DoesNotContain(german, other);
        }

        [Fact]
        public async Task ACookieBeatsTheBrowsersLanguage()
        {
            var (german, french) = SeedSuggestions();

            var page = await HomeAsync(cookie: "tw_languages=fr", acceptLanguage: "de-DE,de;q=0.9");

            Assert.Contains(french, page);
            Assert.DoesNotContain(german, page);
            Assert.DoesNotContain("picked from your browser", page);
        }

        [Fact]
        public async Task ChoosingNoLanguagesStaysChosen()
        {
            var (german, french) = SeedSuggestions();

            //The "none" marker means they chose "all languages", so the browser's language is not used again behind their back.
            var page = await HomeAsync(cookie: "tw_languages=" + LanguagePreferences.NoneMarker, acceptLanguage: "de-DE,de;q=0.9");

            Assert.Contains(german, page);
            Assert.Contains(french, page);
            Assert.DoesNotContain("picked from your browser", page);
            Assert.Contains("Which languages do you read?", page);
        }

        [Fact]
        public async Task AnEnglishOnlyBrowserIsAskedWhichLanguagesItReadsAndIsNotAskedToVoteYet()
        {
            SeedSuggestions();

            var page = await HomeAsync(acceptLanguage: "en-US,en;q=0.9");

            Assert.Contains("Which languages do you read?", page);
            Assert.Contains("Choose your languages first", page);
            Assert.DoesNotContain("Does this sound right?", page);
            Assert.DoesNotContain("picked from your browser", page);
        }

        [Fact]
        public async Task ChosenLanguagesGetTheVoteNowCard()
        {
            SeedSuggestions();

            var page = await HomeAsync(cookie: "tw_languages=de");

            Assert.Contains("Does this sound right?", page);
            Assert.DoesNotContain("Choose your languages first", page);
        }

        [Fact]
        public async Task ACookieThatIsNotALanguageListIsIgnored()
        {
            var (german, french) = SeedSuggestions();

            foreach (var cookie in new[] { "tw_languages=" + Uri.EscapeDataString("<script>alert(1)</script>"), "tw_languages=xx,yy,zz", "tw_languages=" + Uri.EscapeDataString("'; DROP TABLE Suggestion; --") })
            {
                var page = await HomeAsync(cookie: cookie);

                Assert.Contains(german, page);   //nothing valid was chosen, so nothing is filtered out
                Assert.Contains(french, page);
                Assert.DoesNotContain("<script>alert(1)</script>", page);
                Assert.Contains("Which languages do you read?", page);
            }
        }

        [Fact]
        public async Task EveryPageHasTheLanguagePicker()
        {
            foreach (var url in new[] { "/", "/search?q=backup", "/gate" })
            {
                var page = await HomeAsync(url: url);

                Assert.Contains("tw-lang-button", page);
                Assert.Contains("Choose your languages", page);
            }

            var chosen = await HomeAsync(cookie: "tw_languages=" + Uri.EscapeDataString("de,fr"), url: "/search?q=backup");
            Assert.Contains("German, French", chosen); //the button says which
        }

        [Fact]
        public async Task SearchDefaultsToTheVisitorsLanguagesUnlessTheyAskForAll()
        {
            //The phrase "Database Backups" has a translation in every language.
            var mine = await HomeAsync(cookie: "tw_languages=de", url: "/search?q=database%20backups");
            Assert.Contains("My languages (German)", mine);
            Assert.Contains("phrases in German.", mine);

            var all = await HomeAsync(cookie: "tw_languages=de", url: "/search?q=database%20backups&lang=all");
            Assert.DoesNotContain("phrases in German.", all);

            var french = await HomeAsync(cookie: "tw_languages=de", url: "/search?q=database%20backups&lang=French");
            Assert.Contains("phrases in French.", french);

            var none = await HomeAsync(url: "/search?q=database%20backups");
            Assert.DoesNotContain("My languages", none);
            Assert.DoesNotContain(" in German.", none);
        }

        [Fact]
        public async Task AWrongAdminKeyIsRefused()
        {
            using var client = CreateClient();

            var response = await client.PostAsync("/admin/login", Form(("key", "not-the-key")));

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/admin?error=1", response.Headers.Location!.OriginalString);
            Assert.Null(CookieFrom(response));
        }

        [Fact]
        public async Task TheRightAdminKeyOpensTheExports()
        {
            using var client = CreateClient();

            var login = await client.PostAsync("/admin/login", Form(("key", TranslationSiteFactory.AdminKey)));
            Assert.Equal("/admin", login.Headers.Location!.OriginalString);

            var cookie = CookieFrom(login);
            Assert.NotNull(cookie);

            var csv = await client.SendAsync(Get("/admin/export.csv?min=1", cookie));
            Assert.Equal(HttpStatusCode.OK, csv.StatusCode);
            Assert.Equal("text/csv", csv.Content.Headers.ContentType?.MediaType);
            Assert.StartsWith("English,Language,Translation,Score", (await csv.Content.ReadAsStringAsync()).TrimStart('﻿'));

            var json = await client.SendAsync(Get("/admin/export.json?min=1", cookie));
            Assert.Equal(HttpStatusCode.OK, json.StatusCode);
            Assert.Equal("application/json", json.Content.Headers.ContentType?.MediaType);
        }
    }
}
