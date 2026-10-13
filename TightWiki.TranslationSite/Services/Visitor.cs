using System.Security.Claims;

namespace TightWiki.TranslationSite.Services
{
    /// <summary>
    /// Who is looking at the site. There are no accounts: a visitor who passes the captcha is given a cookie
    ///  with a random identifier, which is what their suggestions and votes are recorded against.
    /// </summary>
    public sealed record Visitor(string? Id, bool IsVerified, bool IsAdmin)
    {
        public const string Scheme = "Visitor";
        public const string AdminClaim = "tw:admin";

        public static Visitor Anonymous { get; } = new(null, false, false);

        /// <summary>
        /// The identifier to use for queries, which is empty for visitors who have not been verified.
        /// </summary>
        public string QueryId => Id ?? string.Empty;

        public static Visitor From(ClaimsPrincipal? user)
        {
            if (user?.Identity?.IsAuthenticated != true)
            {
                return Anonymous;
            }

            var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(id))
            {
                return Anonymous;
            }

            return new Visitor(id, true, user.HasClaim(AdminClaim, "true"));
        }

        public static ClaimsPrincipal CreatePrincipal(string id, bool isAdmin)
        {
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, id) };
            if (isAdmin)
            {
                claims.Add(new Claim(AdminClaim, "true"));
            }

            return new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme));
        }
    }

    public static class TimeAgo
    {
        public static string Format(DateTime utc, DateTime? now = null)
        {
            var span = (now ?? DateTime.UtcNow) - utc;

            if (span.TotalSeconds < 45) return "just now";
            if (span.TotalMinutes < 2) return "a minute ago";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} minutes ago";
            if (span.TotalHours < 2) return "an hour ago";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours} hours ago";
            if (span.TotalDays < 2) return "yesterday";
            if (span.TotalDays < 30) return $"{(int)span.TotalDays} days ago";

            return utc.ToString("MMM d, yyyy");
        }
    }
}
