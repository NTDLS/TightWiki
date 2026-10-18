using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;

namespace TightWiki.TranslationSite.Services
{
    /// <summary>
    /// Why a captcha was not accepted.
    /// </summary>
    public enum TurnstileFailure
    {
        None,

        /// <summary>
        /// The site is missing its keys or the list of host names it is allowed to be on.
        /// </summary>
        NotConfigured,

        /// <summary>
        /// There was no token, or it could not possibly be a real one.
        /// </summary>
        NoToken,

        /// <summary>
        /// Cloudflare did not accept the token (it was wrong, expired, already used, or the secret key is wrong).
        /// </summary>
        Rejected,

        /// <summary>
        /// Cloudflare accepted the token, but it was not generated for this page.
        /// </summary>
        WrongAction,

        /// <summary>
        /// Cloudflare accepted the token, but it was generated on a host name that this site is not set up for.
        /// </summary>
        WrongHost,

        /// <summary>
        /// Cloudflare could not be reached or gave an answer that makes no sense.
        /// </summary>
        Unavailable
    }

    public sealed record TurnstileVerdict(TurnstileFailure Failure = TurnstileFailure.None)
    {
        public bool Success => Failure == TurnstileFailure.None;

        /// <summary>
        /// Failures that no visitor can fix by trying again, they mean the site itself is set up wrongly.
        /// </summary>
        public bool IsSetupProblem => Failure is TurnstileFailure.NotConfigured or TurnstileFailure.WrongAction or TurnstileFailure.WrongHost;
    }

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
        public async Task<bool> VerifyAsync(string? token, string? remoteIp, CancellationToken cancellationToken = default)
            => (await VerifyDetailedAsync(token, remoteIp, cancellationToken)).Success;

        /// <summary>
        /// The same check, but says why it failed. The reason is also written to the log with the details that are
        ///  needed to put it right (such as the host name that was not allowed).
        /// </summary>
        public virtual async Task<TurnstileVerdict> VerifyDetailedAsync(string? token, string? remoteIp, CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
            {
                logger.LogError("Turnstile is not configured. Set Turnstile:SiteKey, Turnstile:SecretKey and Turnstile:AllowedHostnames.");
                return new TurnstileVerdict(TurnstileFailure.NotConfigured);
            }

            if (string.IsNullOrEmpty(token) || token.Length > 2048)
            {
                return new TurnstileVerdict(TurnstileFailure.NoToken);
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
                    return new TurnstileVerdict(TurnstileFailure.Unavailable);
                }

                var result = await response.Content.ReadFromJsonAsync<SiteVerifyResponse>(cancellationToken);
                if (result == null || !result.Success)
                {
                    //For example: invalid-input-secret (the secret key does not belong to this widget),
                    //  timeout-or-duplicate (the token was used twice or took too long), or invalid-input-response.
                    logger.LogWarning("Turnstile rejected a token: {Errors}", string.Join(", ", result?.ErrorCodes ?? Array.Empty<string>()));
                    return new TurnstileVerdict(TurnstileFailure.Rejected);
                }

                if (!string.Equals(result.Action, Action, StringComparison.Ordinal))
                {
                    logger.LogWarning("Turnstile token was for action '{Action}' but this site expects '{Expected}'.", result.Action, Action);
                    return new TurnstileVerdict(TurnstileFailure.WrongAction);
                }

                if (!options.Value.AllowedHostnames.Contains(result.Hostname ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                {
                    logger.LogWarning(
                        "Turnstile token was generated on host '{Hostname}', which is not one of the allowed host names ({Allowed}). "
                        + "If that is this site's address, add it to Turnstile:AllowedHostnames.",
                        result.Hostname, string.Join(", ", options.Value.AllowedHostnames));
                    return new TurnstileVerdict(TurnstileFailure.WrongHost);
                }

                return new TurnstileVerdict();
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
            {
                logger.LogWarning(ex, "Turnstile siteverify could not be completed.");
                return new TurnstileVerdict(TurnstileFailure.Unavailable);
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
