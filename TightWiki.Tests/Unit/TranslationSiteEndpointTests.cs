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
            public string? LastToken { get; private set; }

            public override bool IsConfigured => true;

            public override Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken cancellationToken = default)
            {
                LastToken = token;
                return Task.FromResult(Result);
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
            Assert.DoesNotContain("secret-for-tests", text); //the secret key must never reach the browser
        }

        [Fact]
        public async Task AFailedCaptchaGivesNoCookie()
        {
            factory.Verifier.Result = false;
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
