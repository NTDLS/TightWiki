using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace TightWiki.TranslationSite.Services
{
    /// <summary>
    /// Verifies Cloudflare Turnstile tokens. This is always done here on the server, never in the browser.
    /// </summary>
    public class TurnstileVerifier(HttpClient http, IOptions<TurnstileOptions> options, ILogger<TurnstileVerifier> logger)
    {
        /// <summary>
        /// The action the widget is rendered with, so that a token generated for something else is not accepted here.
        /// </summary>
        public const string Action = "verify";

        private const string SiteVerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";

        public string SiteKey => options.Value.SiteKey;

        public virtual bool IsConfigured
            => !string.IsNullOrWhiteSpace(options.Value.SiteKey)
            && !string.IsNullOrWhiteSpace(options.Value.SecretKey)
            && options.Value.AllowedHostnames.Any(o => !string.IsNullOrWhiteSpace(o));

        /// <summary>
        /// Returns true only when Cloudflare confirms the token, for our action, from one of our host names.
        /// Anything else (including being unable to reach Cloudflare) is a failure.
        /// </summary>
        public virtual async Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
            {
                logger.LogError("Turnstile is not configured. Set Turnstile:SiteKey, Turnstile:SecretKey and Turnstile:AllowedHostnames.");
                return false;
            }

            if (string.IsNullOrEmpty(token) || token.Length > 2048)
            {
                return false;
            }

            var form = new Dictionary<string, string>
            {
                ["secret"] = options.Value.SecretKey,
                ["response"] = token
            };

            if (!string.IsNullOrEmpty(remoteIp))
            {
                form["remoteip"] = remoteIp;
            }

            try
            {
                using var response = await http.PostAsync(SiteVerifyUrl, new FormUrlEncodedContent(form), cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("Turnstile siteverify returned {StatusCode}.", (int)response.StatusCode);
                    return false;
                }

                var result = await response.Content.ReadFromJsonAsync<SiteVerifyResponse>(cancellationToken);
                if (result == null || !result.Success)
                {
                    logger.LogInformation("Turnstile rejected a token: {Errors}", string.Join(", ", result?.ErrorCodes ?? Array.Empty<string>()));
                    return false;
                }

                if (!string.Equals(result.Action, Action, StringComparison.Ordinal))
                {
                    logger.LogWarning("Turnstile token was for action '{Action}', expected '{Expected}'.", result.Action, Action);
                    return false;
                }

                if (!options.Value.AllowedHostnames.Contains(result.Hostname ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                {
                    logger.LogWarning("Turnstile token came from host '{Hostname}' which is not allowed.", result.Hostname);
                    return false;
                }

                return true;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
            {
                logger.LogWarning(ex, "Turnstile siteverify could not be completed.");
                return false;
            }
        }

        private sealed class SiteVerifyResponse
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }

            [JsonPropertyName("hostname")]
            public string? Hostname { get; set; }

            [JsonPropertyName("action")]
            public string? Action { get; set; }

            [JsonPropertyName("error-codes")]
            public string[]? ErrorCodes { get; set; }
        }
    }
}
