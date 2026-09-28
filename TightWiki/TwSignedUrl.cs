using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;
using System.Text.Json;

namespace TightWiki
{
    /// <summary>
    /// Builds and reads tamper-proof, expiring tokens for pages whose content must come from the server,
    /// such as the notification and confirm-action pages. Anything read from these tokens was written by
    /// the wiki itself, so a crafted link can not inject markup, messages or targets into those pages.
    /// </summary>
    public static class TwSignedUrl
    {
        private static ITimeLimitedDataProtector? _protector;

        private static ITimeLimitedDataProtector Protector
            => _protector ?? throw new InvalidOperationException("TwSignedUrl has not been initialized.");

        public static void Initialize(IDataProtectionProvider provider)
            => _protector = provider.CreateProtector("TightWiki.SignedUrl.v1").ToTimeLimitedDataProtector();

        /// <summary>
        /// Returns a URL-safe token containing the payload, which expires after the given lifetime.
        /// </summary>
        public static string Protect<T>(T payload, TimeSpan lifetime)
            => Uri.EscapeDataString(Protector.Protect(JsonSerializer.Serialize(payload), lifetime));

        /// <summary>
        /// Reads a token created by Protect(), returning false if it is missing, altered or expired.
        /// </summary>
        public static bool TryUnprotect<T>(string? token, out T? payload)
        {
            payload = default;
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            try
            {
                payload = JsonSerializer.Deserialize<T>(Protector.Unprotect(token, out _));
                return payload != null;
            }
            catch (CryptographicException)
            {
                return false;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// The content of the notification page.
    /// </summary>
    public record TwNotifyPayload(string? Success, string? Warning, string? Error, string? RedirectUrl, int RedirectTimeout);

    /// <summary>
    /// The content of the confirm-action page.
    /// </summary>
    public record TwConfirmActionPayload(string ControllerURL, string YesRedirectURL, string NoRedirectURL, string Message, string Style);

    public static class TwNotifyUrl
    {
        private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Builds a signed URL to the notification page. The redirect URL, if any, must already include the base path.
        /// </summary>
        public static string Build(string basePath, string? success = null, string? warning = null, string? error = null,
            string? redirectUrl = null, int redirectTimeout = 0)
            => $"{basePath}/Utility/Notify?t={TwSignedUrl.Protect(new TwNotifyPayload(success, warning, error, redirectUrl, redirectTimeout), Lifetime)}";
    }
}
