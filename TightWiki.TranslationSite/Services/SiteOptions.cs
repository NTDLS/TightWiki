namespace TightWiki.TranslationSite.Services
{
    /// <summary>
    /// Settings for the translations site (the "Site" section of appsettings.json).
    /// </summary>
    public class SiteOptions
    {
        /// <summary>
        /// The read-only translation database. Defaults to the copy that is published with the site.
        /// </summary>
        public string TranslationsPath { get; set; } = string.Empty;

        /// <summary>
        /// The database that holds suggestions and votes. It is created on first use and is never replaced by a publish.
        /// </summary>
        public string SuggestionsPath { get; set; } = string.Empty;

        /// <summary>
        /// Suggestions are hidden once their score (up-votes minus down-votes) drops to this value or lower,
        ///  unless an administrator has approved them.
        /// </summary>
        public int AutoHideScore { get; set; } = -5;

        public int MaxSuggestionLength { get; set; } = 500;

        /// <summary>
        /// How many suggestions a single visitor can make per hour.
        /// </summary>
        public int SuggestionsPerHour { get; set; } = 10;

        /// <summary>
        /// How many votes a single visitor can cast per minute.
        /// </summary>
        public int VotesPerMinute { get; set; } = 40;

        /// <summary>
        /// The key that gives access to the admin page. When empty the admin page is disabled.
        /// </summary>
        public string AdminKey { get; set; } = string.Empty;

        /// <summary>
        /// How long an admin sign-in lasts.
        /// </summary>
        public int AdminSessionHours { get; set; } = 8;

        /// <summary>
        /// How many captcha (and admin key) attempts one address can make per minute.
        /// </summary>
        public int GateAttemptsPerMinute { get; set; } = 10;

        /// <summary>
        /// How many days a visitor stays verified after passing the captcha.
        /// </summary>
        public int VisitorDays { get; set; } = 30;
    }

    /// <summary>
    /// Cloudflare Turnstile settings (the "Turnstile" section of appsettings.json).
    /// </summary>
    public class TurnstileOptions
    {
        /// <summary>
        /// The public key that is embedded in the page.
        /// </summary>
        public string SiteKey { get; set; } = string.Empty;

        /// <summary>
        /// The secret key used to verify tokens. This must never be committed, supply it with user-secrets
        ///  or the environment variable Turnstile__SecretKey.
        /// </summary>
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// The host names that a verified token is allowed to have come from. Cloudflare reports the host the
        ///  widget was shown on, so a token generated elsewhere is rejected. Production must not include localhost.
        /// </summary>
        public string[] AllowedHostnames { get; set; } = Array.Empty<string>();
    }
}
