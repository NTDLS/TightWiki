namespace TightWiki.Plugin.Styler
{
    /// <summary>
    /// Background and Foreground style.
    /// </summary>
    public class TwFillStyler
    {
        /// <summary>
        /// Returns the forground style for the given style.
        /// </summary>
        public string ForegroundStyle { get; set; } = string.Empty;

        /// <summary>
        /// Returns the background style for the given style.
        /// </summary>
        public string BackgroundStyle { get; set; } = string.Empty;

        /// <summary>
        /// Background and Foreground style.
        /// </summary>
        public TwFillStyler(string foregroundStyle, string backgroundStyle)
        {
            ForegroundStyle = foregroundStyle;
            BackgroundStyle = backgroundStyle;
        }

        /// <summary>
        /// Returns a new instances where the background and forground styles have been swapped.
        /// </summary>
        public TwFillStyler Swap()
        {
            return new TwFillStyler(BackgroundStyle, ForegroundStyle);
        }

        /// <summary>
        /// Background and Foreground style.
        /// </summary>
        public TwFillStyler()
        {
        }

        private static readonly Dictionary<TwBootstrapStyle, TwFillStyler> ForegroundStyles = new()
        {
            { TwBootstrapStyle.Primary, new TwFillStyler("text-primary", "") },
            { TwBootstrapStyle.Secondary, new TwFillStyler("text-secondary", "") },
            { TwBootstrapStyle.Success, new TwFillStyler("text-success", "") },
            { TwBootstrapStyle.Danger, new TwFillStyler("text-danger", "") },
            { TwBootstrapStyle.Warning, new TwFillStyler("text-warning", "") },
            { TwBootstrapStyle.Info, new TwFillStyler("text-info", "") },
            { TwBootstrapStyle.Light, new TwFillStyler("text-light", "") },
            { TwBootstrapStyle.Dark, new TwFillStyler("text-dark", "") },
            { TwBootstrapStyle.Muted, new TwFillStyler("text-muted", "") },
            { TwBootstrapStyle.White, new TwFillStyler("text-white", "bg-dark") }
        };

        private static readonly Dictionary<TwBootstrapStyle, TwFillStyler> BackgroundStyles = new()
        {
            { TwBootstrapStyle.Muted, new TwFillStyler("text-muted", "") },
            { TwBootstrapStyle.Primary, new TwFillStyler("text-white", "bg-primary") },
            { TwBootstrapStyle.Secondary, new TwFillStyler("text-white", "bg-secondary") },
            { TwBootstrapStyle.Info, new TwFillStyler("text-white", "bg-info") },
            { TwBootstrapStyle.Success, new TwFillStyler("text-white", "bg-success") },
            { TwBootstrapStyle.Warning, new TwFillStyler("bg-warning", "") },
            { TwBootstrapStyle.Danger, new TwFillStyler("text-white", "bg-danger") },
            { TwBootstrapStyle.Light, new TwFillStyler("text-black", "bg-light") },
            { TwBootstrapStyle.Dark, new TwFillStyler("text-white", "bg-dark") }
        };

        /// <summary>
        /// Gets the background style for the given style.
        /// </summary>
        public static TwFillStyler GetBackgroundStyle(TwBootstrapStyle style)
        {
            if (BackgroundStyles.TryGetValue(style, out var html))
            {
                return html;
            }

            return new TwFillStyler();
        }

        /// <summary>
        /// Gets the foreground style for the given style.
        /// </summary>
        public static TwFillStyler GetForegroundStyle(TwBootstrapStyle style)
        {
            if (ForegroundStyles.TryGetValue(style, out var html))
            {
                return html;
            }

            return new TwFillStyler();
        }
    }
}
