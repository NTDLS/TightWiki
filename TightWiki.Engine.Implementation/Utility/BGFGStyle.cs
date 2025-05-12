namespace TightWiki.Engine.Implementation.Utility
{
    /// <summary>
    /// Background and Foreground style.
    /// </summary>
    public class BGFGStyle
    {
        public string ForegroundStyle { get; set; } = string.Empty;
        public string BackgroundStyle { get; set; } = string.Empty;

        public BGFGStyle(string foregroundStyle, string backgroundStyle)
        {
            ForegroundStyle = foregroundStyle;
            BackgroundStyle = backgroundStyle;
        }

        public BGFGStyle Swap()
        {
            return new BGFGStyle(BackgroundStyle, ForegroundStyle);
        }

        public BGFGStyle()
        {
        }

        public static readonly Dictionary<string, BGFGStyle> ForegroundStyles = new(StringComparer.InvariantCultureIgnoreCase)
        {
            { "primary", new BGFGStyle("text-primary", "") },
            { "secondary", new BGFGStyle("text-secondary", "") },
            { "success", new BGFGStyle("text-success", "") },
            { "danger", new BGFGStyle("text-danger", "") },
            { "warning", new BGFGStyle("text-warning", "") },
            { "info", new BGFGStyle("text-info", "") },
            { "light", new BGFGStyle("text-light", "") },
            { "dark", new BGFGStyle("text-dark", "") },
            { "muted", new BGFGStyle("text-muted", "") },
            { "white", new BGFGStyle("text-white", "bg-dark") }
        };

        public static readonly Dictionary<string, BGFGStyle> BackgroundStyles = new(StringComparer.InvariantCultureIgnoreCase)
        {
            { "muted", new BGFGStyle("text-muted", "") },
            { "primary", new BGFGStyle("text-white", "bg-primary") },
            { "secondary", new BGFGStyle("text-white", "bg-secondary") },
            { "info", new BGFGStyle("text-white", "bg-info") },
            { "success", new BGFGStyle("text-white", "bg-success") },
            { "warning", new BGFGStyle("bg-warning", "") },
            { "danger", new BGFGStyle("text-white", "bg-danger") },
            { "light", new BGFGStyle("text-black", "bg-light") },
            { "dark", new BGFGStyle("text-white", "bg-dark") }
        };

        public static BGFGStyle GetBackgroundStyle(string style)
        {
            if (BackgroundStyles.TryGetValue(style, out var html))
            {
                return html;
            }

            return new BGFGStyle();
        }

        public static BGFGStyle GetForegroundStyle(string style)
        {
            if (ForegroundStyles.TryGetValue(style, out var html))
            {
                return html;
            }

            return new BGFGStyle();
        }
    }
}
