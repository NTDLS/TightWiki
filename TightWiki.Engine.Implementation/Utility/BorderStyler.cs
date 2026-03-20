namespace TightWiki.Engine.Implementation.Utility
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

        public static readonly Dictionary<string, BorderStyler> ForegroundStyles = new(StringComparer.InvariantCultureIgnoreCase)
        {
            { "primary", new BorderStyler("text-primary", "") },
            { "secondary", new BorderStyler("text-secondary", "") },
            { "success", new BorderStyler("text-success", "") },
            { "danger", new BorderStyler("text-danger", "") },
            { "warning", new BorderStyler("text-warning", "") },
            { "info", new BorderStyler("text-info", "") },
            { "light", new BorderStyler("text-light", "") },
            { "dark", new BorderStyler("text-dark", "") },
            { "muted", new BorderStyler("text-muted", "") },
            { "white", new BorderStyler("text-white", "bg-dark") }
        };

        public static readonly Dictionary<string, BorderStyler> BorderStyles = new(StringComparer.InvariantCultureIgnoreCase)
        {
            { "muted", new BorderStyler("text-muted", "") },
            { "primary", new BorderStyler("text-dark", "border-primary") },
            { "secondary", new BorderStyler("text-dark", "border-secondary") },
            { "info", new BorderStyler("text-dark", "border-info") },
            { "success", new BorderStyler("text-dark", "border-success") },
            { "warning", new BorderStyler("text-dark", "border-warning") },
            { "danger", new BorderStyler("text-dark", "border-danger") },
            { "light", new BorderStyler("text-dark", "border-light") },
            { "dark", new BorderStyler("text-dark", "border-dark") }
        };

        public static BorderStyler GetBorderStyle(string style)
        {
            if (BorderStyles.TryGetValue(style, out var html))
            {
                return html;
            }

            return new BorderStyler();
        }

        public static BorderStyler GetForegroundStyle(string style)
        {
            if (ForegroundStyles.TryGetValue(style, out var html))
            {
                return html;
            }

            return new BorderStyler();
        }
    }
}
