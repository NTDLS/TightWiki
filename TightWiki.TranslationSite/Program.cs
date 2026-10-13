using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using TightWiki.TranslationSite.Components;
using TightWiki.TranslationSite.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<SiteOptions>(builder.Configuration.GetSection("Site"));
builder.Services.Configure<TurnstileOptions>(builder.Configuration.GetSection("Turnstile"));
builder.Services.PostConfigure<SiteOptions>(options =>
{
    if (string.IsNullOrWhiteSpace(options.TranslationsPath))
    {
        options.TranslationsPath = Path.Combine(AppContext.BaseDirectory, "Data", "Translations.db");
    }

    if (string.IsNullOrWhiteSpace(options.SuggestionsPath))
    {
        options.SuggestionsPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "Suggestions.db");
    }
});

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(sp => new TranslationCatalog(sp.GetRequiredService<IOptions<SiteOptions>>().Value.TranslationsPath));
builder.Services.AddSingleton<SuggestionService>();
builder.Services.AddHttpClient<TurnstileVerifier>(client => client.Timeout = TimeSpan.FromSeconds(10));

var site = builder.Configuration.GetSection("Site").Get<SiteOptions>() ?? new SiteOptions();

//There are no accounts. Passing the captcha gives the visitor a cookie holding a random identifier,
//  and that identifier is what suggestions and votes are recorded against.
builder.Services.AddAuthentication(Visitor.Scheme)
    .AddCookie(Visitor.Scheme, options =>
    {
        options.Cookie.Name = "tw_translations";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromDays(site.VisitorDays);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
        options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Admin", policy => policy
        .AddAuthenticationSchemes(Visitor.Scheme)
        .RequireClaim(Visitor.AdminClaim, "true"));

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("gate", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = Math.Max(1, site.GateAttemptsPerMinute), Window = TimeSpan.FromMinutes(1) }));
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);

var app = builder.Build();

//Fail at startup, not on the first visit, when the translations are missing.
_ = app.Services.GetRequiredService<TranslationCatalog>();

if (string.IsNullOrWhiteSpace(app.Configuration["Turnstile:SecretKey"]))
{
    app.Logger.LogWarning("Turnstile:SecretKey is not set, so nobody will be able to pass the captcha. Use 'dotnet user-secrets' or the Turnstile__SecretKey environment variable.");
}

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

//The visitor passes the captcha on the /gate page, which posts here (to its own path, as /gate is the page). The token is verified with Cloudflare
//  by the server (never the browser) before the visitor is given a cookie.
app.MapPost("/gate/verify", async (HttpContext context, TurnstileVerifier verifier, IOptions<SiteOptions> options) =>
{
    var form = await context.Request.ReadFormAsync();
    var returnUrl = SafeLocalUrl(form["returnUrl"]);

    var verified = await verifier.VerifyAsync(form["cf-turnstile-response"], context.Connection.RemoteIpAddress?.ToString(), context.RequestAborted);
    if (!verified)
    {
        return Results.Redirect($"/gate?error=1&returnUrl={Uri.EscapeDataString(returnUrl)}");
    }

    var current = Visitor.From(context.User);
    var principal = Visitor.CreatePrincipal(current.Id ?? Guid.NewGuid().ToString("N"), current.IsAdmin);

    await context.SignInAsync(Visitor.Scheme, principal, new AuthenticationProperties
    {
        IsPersistent = true,
        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(options.Value.VisitorDays)
    });

    return Results.Redirect(returnUrl);
}).RequireRateLimiting("gate").DisableAntiforgery();

//The admin page is opened with a secret key. There is no other account on the site.
app.MapPost("/admin/login", async (HttpContext context, IOptions<SiteOptions> options) =>
{
    var expected = options.Value.AdminKey;
    if (string.IsNullOrEmpty(expected))
    {
        return Results.NotFound();
    }

    var form = await context.Request.ReadFormAsync();
    var supplied = form["key"].ToString();

    //Comparing hashes means the comparison takes the same time however much of the key is right.
    var matches = CryptographicOperations.FixedTimeEquals(
        SHA256.HashData(Encoding.UTF8.GetBytes(supplied)),
        SHA256.HashData(Encoding.UTF8.GetBytes(expected)));

    if (!matches)
    {
        return Results.Redirect("/admin?error=1");
    }

    var current = Visitor.From(context.User);
    await context.SignInAsync(Visitor.Scheme, Visitor.CreatePrincipal(current.Id ?? Guid.NewGuid().ToString("N"), isAdmin: true), new AuthenticationProperties
    {
        IsPersistent = false,
        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(options.Value.AdminSessionHours)
    });

    return Results.Redirect("/admin");
}).RequireRateLimiting("gate").DisableAntiforgery();

app.MapPost("/admin/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(Visitor.Scheme);
    return Results.Redirect("/");
}).DisableAntiforgery();

//The best suggestion for each phrase and language, ready to be merged back into TightWiki's translations.
app.MapGet("/admin/export.csv", (SuggestionService suggestions, int? min) =>
{
    var builder = new StringBuilder("English,Language,Translation,Score\r\n");
    foreach (var item in suggestions.GetTopSuggestions(min ?? 3))
    {
        builder.Append(Csv(item.English)).Append(',').Append(Csv(item.Language)).Append(',').Append(Csv(item.Text)).Append(',')
            .Append(item.Score.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
    }

    return Results.File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray(), "text/csv", "translation-suggestions.csv");
}).RequireAuthorization("Admin");

app.MapGet("/admin/export.json", (SuggestionService suggestions, int? min) =>
{
    var items = suggestions.GetTopSuggestions(min ?? 3)
        .Select(o => new { o.English, o.Language, Translation = o.Text, o.Score })
        .ToList();

    return Results.File(JsonSerializer.SerializeToUtf8Bytes(items, new JsonSerializerOptions { WriteIndented = true }), "application/json", "translation-suggestions.json");
}).RequireAuthorization("Admin");

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();

/// <summary>
/// Only allows redirects to a path on this site, so that the captcha cannot be used to send people elsewhere.
/// </summary>
static string SafeLocalUrl(string? url)
    => !string.IsNullOrEmpty(url) && url[0] == '/' && !url.StartsWith("//", StringComparison.Ordinal) && !url.StartsWith("/\\", StringComparison.Ordinal)
        ? url
        : "/";

static string Csv(string value)
    => "\"" + value.Replace("\"", "\"\"") + "\"";

public partial class Program;
