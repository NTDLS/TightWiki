namespace TightWiki.Plugin
{
    /// <summary>
    /// Background and Foreground style.
    /// </summary>
    public class TwFillStyler
    {
        public string ForegroundStyle { get; set; } = string.Empty;
        public string BackgroundStyle { get; set; } = string.Empty;

        public TwFillStyler(string foregroundStyle, string backgroundStyle)
        {
            ForegroundStyle = foregroundStyle;
            BackgroundStyle = backgroundStyle;
        }

        public TwFillStyler Swap()
        {
            return new TwFillStyler(BackgroundStyle, ForegroundStyle);
        }

        public TwFillStyler()
        {
        }

        public static readonly Dictionary<TightWikiBootstrapStyle, TwFillStyler> ForegroundStyles = new()
        {
            { TightWikiBootstrapStyle.Primary, new TwFillStyler("text-primary", "") },
            { TightWikiBootstrapStyle.Secondary, new TwFillStyler("text-secondary", "") },
            { TightWikiBootstrapStyle.Success, new TwFillStyler("text-success", "") },
            { TightWikiBootstrapStyle.Danger, new TwFillStyler("text-danger", "") },
            { TightWikiBootstrapStyle.Warning, new TwFillStyler("text-warning", "") },
            { TightWikiBootstrapStyle.Info, new TwFillStyler("text-info", "") },
            { TightWikiBootstrapStyle.Light, new TwFillStyler("text-light", "") },
            { TightWikiBootstrapStyle.Dark, new TwFillStyler("text-dark", "") },
            { TightWikiBootstrapStyle.Muted, new TwFillStyler("text-muted", "") },
            { TightWikiBootstrapStyle.White, new TwFillStyler("text-white", "bg-dark") }
        };

        public static readonly Dictionary<TightWikiBootstrapStyle, TwFillStyler> BackgroundStyles = new()
        {
            { TightWikiBootstrapStyle.Muted, new TwFillStyler("text-muted", "") },
            { TightWikiBootstrapStyle.Primary, new TwFillStyler("text-white", "bg-primary") },
            { TightWikiBootstrapStyle.Secondary, new TwFillStyler("text-white", "bg-secondary") },
            { TightWikiBootstrapStyle.Info, new TwFillStyler("text-white", "bg-info") },
            { TightWikiBootstrapStyle.Success, new TwFillStyler("text-white", "bg-success") },
            { TightWikiBootstrapStyle.Warning, new TwFillStyler("bg-warning", "") },
            { TightWikiBootstrapStyle.Danger, new TwFillStyler("text-white", "bg-danger") },
            { TightWikiBootstrapStyle.Light, new TwFillStyler("text-black", "bg-light") },
            { TightWikiBootstrapStyle.Dark, new TwFillStyler("text-white", "bg-dark") }
        };

        public static TwFillStyler GetBackgroundStyle(TightWikiBootstrapStyle style)
        {
            if (BackgroundStyles.TryGetValue(style, out var html))
            {
                return html;
            }

            return new TwFillStyler();
        }

        public static TwFillStyler GetForegroundStyle(TightWikiBootstrapStyle style)
        {
            if (ForegroundStyles.TryGetValue(style, out var html))
            {
                return html;
            }

            return new TwFillStyler();
        }
    }
}
