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

        public static readonly Dictionary<string, FillStyler> ForegroundStyles = new(StringComparer.InvariantCultureIgnoreCase)
        {
            { "primary", new FillStyler("text-primary", "") },
            { "secondary", new FillStyler("text-secondary", "") },
            { "success", new FillStyler("text-success", "") },
            { "danger", new FillStyler("text-danger", "") },
            { "warning", new FillStyler("text-warning", "") },
            { "info", new FillStyler("text-info", "") },
            { "light", new FillStyler("text-light", "") },
            { "dark", new FillStyler("text-dark", "") },
            { "muted", new FillStyler("text-muted", "") },
            { "white", new FillStyler("text-white", "bg-dark") }
        };

        public static readonly Dictionary<string, FillStyler> BackgroundStyles = new(StringComparer.InvariantCultureIgnoreCase)
        {
            { "muted", new FillStyler("text-muted", "") },
            { "primary", new FillStyler("text-white", "bg-primary") },
            { "secondary", new FillStyler("text-white", "bg-secondary") },
            { "info", new FillStyler("text-white", "bg-info") },
            { "success", new FillStyler("text-white", "bg-success") },
            { "warning", new FillStyler("bg-warning", "") },
            { "danger", new FillStyler("text-white", "bg-danger") },
            { "light", new FillStyler("text-black", "bg-light") },
            { "dark", new FillStyler("text-white", "bg-dark") }
        };

        public static FillStyler GetBackgroundStyle(string style)
        {
            if (BackgroundStyles.TryGetValue(style, out var html))
            {
                return html;
            }

            return new FillStyler();
        }

        public static FillStyler GetForegroundStyle(string style)
        {
            if (ForegroundStyles.TryGetValue(style, out var html))
            {
                return html;
            }

            return new FillStyler();
        }
    }
}
