namespace TightWiki.Plugin
{
    /// <summary>
    /// Border and Foreground style.
    /// </summary>
    public class TwBorderStyler
    {
        public string ForegroundStyle { get; set; } = string.Empty;
        public string BorderStyle { get; set; } = string.Empty;

        public TwBorderStyler(string foregroundStyle, string borderStyle)
        {
            ForegroundStyle = foregroundStyle;
            BorderStyle = borderStyle;
        }

        public TwBorderStyler Swap()
        {
            return new TwBorderStyler(BorderStyle, ForegroundStyle);
        }

        public TwBorderStyler()
        {
        }

        public static readonly Dictionary<TightWikiBootstrapStyle, TwBorderStyler> ForegroundStyles = new()
        {
            { TightWikiBootstrapStyle.Primary, new TwBorderStyler("text-primary", "") },
            { TightWikiBootstrapStyle.Secondary, new TwBorderStyler("text-secondary", "") },
            { TightWikiBootstrapStyle.Success, new TwBorderStyler("text-success", "") },
            { TightWikiBootstrapStyle.Danger, new TwBorderStyler("text-danger", "") },
            { TightWikiBootstrapStyle.Warning, new TwBorderStyler("text-warning", "") },
            { TightWikiBootstrapStyle.Info, new TwBorderStyler("text-info", "") },
            { TightWikiBootstrapStyle.Light, new TwBorderStyler("text-light", "") },
            { TightWikiBootstrapStyle.Dark, new TwBorderStyler("text-dark", "") },
            { TightWikiBootstrapStyle.Muted, new TwBorderStyler("text-muted", "") },
            { TightWikiBootstrapStyle.White, new TwBorderStyler("text-white", "bg-dark") }
        };

        public static readonly Dictionary<TightWikiBootstrapStyle, TwBorderStyler> BorderStyles = new()
        {
            { TightWikiBootstrapStyle.Muted, new TwBorderStyler("text-muted", "") },
            { TightWikiBootstrapStyle.Primary, new TwBorderStyler("text-dark", "border-primary") },
            { TightWikiBootstrapStyle.Secondary, new TwBorderStyler("text-dark", "border-secondary") },
            { TightWikiBootstrapStyle.Info, new TwBorderStyler("text-dark", "border-info") },
            { TightWikiBootstrapStyle.Success, new TwBorderStyler("text-dark", "border-success") },
            { TightWikiBootstrapStyle.Warning, new TwBorderStyler("text-dark", "border-warning") },
            { TightWikiBootstrapStyle.Danger, new TwBorderStyler("text-dark", "border-danger") },
            { TightWikiBootstrapStyle.Light, new TwBorderStyler("text-dark", "border-light") },
            { TightWikiBootstrapStyle.Dark, new TwBorderStyler("text-dark", "border-dark") }
        };

        public static TwBorderStyler GetBorderStyle(TightWikiBootstrapStyle style)
        {
            if (BorderStyles.TryGetValue(style, out var html))
            {
                return html;
            }

            return new TwBorderStyler();
        }

        public static TwBorderStyler GetForegroundStyle(TightWikiBootstrapStyle style)
        {
            if (ForegroundStyles.TryGetValue(style, out var html))
            {
                return html;
            }

            return new TwBorderStyler();
        }
    }
}
