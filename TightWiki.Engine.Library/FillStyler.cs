namespace TightWiki.Engine.Implementation.Utility
{
    /// <summary>
    /// Background and Foreground style.
    /// </summary>
    public class FillStyler
    {
        public string ForegroundStyle { get; set; } = string.Empty;
        public string BackgroundStyle { get; set; } = string.Empty;

        public FillStyler(string foregroundStyle, string backgroundStyle)
        {
            ForegroundStyle = foregroundStyle;
            BackgroundStyle = backgroundStyle;
        }

        public FillStyler Swap()
        {
            return new FillStyler(BackgroundStyle, ForegroundStyle);
        }

        public FillStyler()
        {
        }

        public static readonly Dictionary<TightWikiBootstrapStyle, FillStyler> ForegroundStyles = new()
        {
            { TightWikiBootstrapStyle.Primary, new FillStyler("text-primary", "") },
            { TightWikiBootstrapStyle.Secondary, new FillStyler("text-secondary", "") },
            { TightWikiBootstrapStyle.Success, new FillStyler("text-success", "") },
            { TightWikiBootstrapStyle.Danger, new FillStyler("text-danger", "") },
            { TightWikiBootstrapStyle.Warning, new FillStyler("text-warning", "") },
            { TightWikiBootstrapStyle.Info, new FillStyler("text-info", "") },
            { TightWikiBootstrapStyle.Light, new FillStyler("text-light", "") },
            { TightWikiBootstrapStyle.Dark, new FillStyler("text-dark", "") },
            { TightWikiBootstrapStyle.Muted, new FillStyler("text-muted", "") },
            { TightWikiBootstrapStyle.White, new FillStyler("text-white", "bg-dark") }
        };

        public static readonly Dictionary<TightWikiBootstrapStyle, FillStyler> BackgroundStyles = new()
        {
            { TightWikiBootstrapStyle.Muted, new FillStyler("text-muted", "") },
            { TightWikiBootstrapStyle.Primary, new FillStyler("text-white", "bg-primary") },
            { TightWikiBootstrapStyle.Secondary, new FillStyler("text-white", "bg-secondary") },
            { TightWikiBootstrapStyle.Info, new FillStyler("text-white", "bg-info") },
            { TightWikiBootstrapStyle.Success, new FillStyler("text-white", "bg-success") },
            { TightWikiBootstrapStyle.Warning, new FillStyler("bg-warning", "") },
            { TightWikiBootstrapStyle.Danger, new FillStyler("text-white", "bg-danger") },
            { TightWikiBootstrapStyle.Light, new FillStyler("text-black", "bg-light") },
            { TightWikiBootstrapStyle.Dark, new FillStyler("text-white", "bg-dark") }
        };

        public static FillStyler GetBackgroundStyle(TightWikiBootstrapStyle style)
        {
            if (BackgroundStyles.TryGetValue(style, out var html))
            {
                return html;
            }

            return new FillStyler();
        }

        public static FillStyler GetForegroundStyle(TightWikiBootstrapStyle style)
        {
            if (ForegroundStyles.TryGetValue(style, out var html))
            {
                return html;
            }

            return new FillStyler();
        }
    }
}
