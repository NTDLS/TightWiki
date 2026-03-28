namespace TightWiki.Engine.Library
{
    /// <summary>
    /// Border and Foreground style.
    /// </summary>
    public class BorderStyler
    {
        public string ForegroundStyle { get; set; } = string.Empty;
        public string BorderStyle { get; set; } = string.Empty;

        public BorderStyler(string foregroundStyle, string borderStyle)
        {
            ForegroundStyle = foregroundStyle;
            BorderStyle = borderStyle;
        }

        public BorderStyler Swap()
        {
            return new BorderStyler(BorderStyle, ForegroundStyle);
        }

        public BorderStyler()
        {
        }

        public static readonly Dictionary<TightWikiBootstrapStyle, BorderStyler> ForegroundStyles = new()
        {
            { TightWikiBootstrapStyle.Primary, new BorderStyler("text-primary", "") },
            { TightWikiBootstrapStyle.Secondary, new BorderStyler("text-secondary", "") },
            { TightWikiBootstrapStyle.Success, new BorderStyler("text-success", "") },
            { TightWikiBootstrapStyle.Danger, new BorderStyler("text-danger", "") },
            { TightWikiBootstrapStyle.Warning, new BorderStyler("text-warning", "") },
            { TightWikiBootstrapStyle.Info, new BorderStyler("text-info", "") },
            { TightWikiBootstrapStyle.Light, new BorderStyler("text-light", "") },
            { TightWikiBootstrapStyle.Dark, new BorderStyler("text-dark", "") },
            { TightWikiBootstrapStyle.Muted, new BorderStyler("text-muted", "") },
            { TightWikiBootstrapStyle.White, new BorderStyler("text-white", "bg-dark") }
        };

        public static readonly Dictionary<TightWikiBootstrapStyle, BorderStyler> BorderStyles = new()
        {
            { TightWikiBootstrapStyle.Muted, new BorderStyler("text-muted", "") },
            { TightWikiBootstrapStyle.Primary, new BorderStyler("text-dark", "border-primary") },
            { TightWikiBootstrapStyle.Secondary, new BorderStyler("text-dark", "border-secondary") },
            { TightWikiBootstrapStyle.Info, new BorderStyler("text-dark", "border-info") },
            { TightWikiBootstrapStyle.Success, new BorderStyler("text-dark", "border-success") },
            { TightWikiBootstrapStyle.Warning, new BorderStyler("text-dark", "border-warning") },
            { TightWikiBootstrapStyle.Danger, new BorderStyler("text-dark", "border-danger") },
            { TightWikiBootstrapStyle.Light, new BorderStyler("text-dark", "border-light") },
            { TightWikiBootstrapStyle.Dark, new BorderStyler("text-dark", "border-dark") }
        };

        public static BorderStyler GetBorderStyle(TightWikiBootstrapStyle style)
        {
            if (BorderStyles.TryGetValue(style, out var html))
            {
                return html;
            }

            return new BorderStyler();
        }

        public static BorderStyler GetForegroundStyle(TightWikiBootstrapStyle style)
        {
            if (ForegroundStyles.TryGetValue(style, out var html))
            {
                return html;
            }

            return new BorderStyler();
        }
    }
}
