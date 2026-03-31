namespace TightWiki.Plugin.Styler
{
    /// <summary>
    /// Border and Foreground style.
    /// </summary>
    public class TwBorderStyler
    {
        /// <summary>
        /// Returns the forground style for the given style.
        /// </summary>
        public string ForegroundStyle { get; set; } = string.Empty;

        /// <summary>
        /// Returns the border style for the given style.
        /// </summary>
        public string BorderStyle { get; set; } = string.Empty;

        /// <summary>
        /// Border and Foreground style.
        /// </summary>
        public TwBorderStyler(string foregroundStyle, string borderStyle)
        {
            ForegroundStyle = foregroundStyle;
            BorderStyle = borderStyle;
        }

        /// <summary>
        /// Returns a new instances where the border and forground styles have been swapped.
        /// </summary>
        public TwBorderStyler Swap()
        {
            return new TwBorderStyler(BorderStyle, ForegroundStyle);
        }

        /// <summary>
        /// Border and Foreground style.
        /// </summary>
        public TwBorderStyler()
        {
        }

        private static readonly Dictionary<TwBootstrapStyle, TwBorderStyler> ForegroundStyles = new()
        {
            { TwBootstrapStyle.Primary, new TwBorderStyler("text-primary", "") },
            { TwBootstrapStyle.Secondary, new TwBorderStyler("text-secondary", "") },
            { TwBootstrapStyle.Success, new TwBorderStyler("text-success", "") },
            { TwBootstrapStyle.Danger, new TwBorderStyler("text-danger", "") },
            { TwBootstrapStyle.Warning, new TwBorderStyler("text-warning", "") },
            { TwBootstrapStyle.Info, new TwBorderStyler("text-info", "") },
            { TwBootstrapStyle.Light, new TwBorderStyler("text-light", "") },
            { TwBootstrapStyle.Dark, new TwBorderStyler("text-dark", "") },
            { TwBootstrapStyle.Muted, new TwBorderStyler("text-muted", "") },
            { TwBootstrapStyle.White, new TwBorderStyler("text-white", "bg-dark") }
        };

        private static readonly Dictionary<TwBootstrapStyle, TwBorderStyler> BorderStyles = new()
        {
            { TwBootstrapStyle.Muted, new TwBorderStyler("text-muted", "") },
            { TwBootstrapStyle.Primary, new TwBorderStyler("text-dark", "border-primary") },
            { TwBootstrapStyle.Secondary, new TwBorderStyler("text-dark", "border-secondary") },
            { TwBootstrapStyle.Info, new TwBorderStyler("text-dark", "border-info") },
            { TwBootstrapStyle.Success, new TwBorderStyler("text-dark", "border-success") },
            { TwBootstrapStyle.Warning, new TwBorderStyler("text-dark", "border-warning") },
            { TwBootstrapStyle.Danger, new TwBorderStyler("text-dark", "border-danger") },
            { TwBootstrapStyle.Light, new TwBorderStyler("text-dark", "border-light") },
            { TwBootstrapStyle.Dark, new TwBorderStyler("text-dark", "border-dark") }
        };

        /// <summary>
        /// Gets the border style for the given style.
        /// </summary>
        public static TwBorderStyler GetBorderStyle(TwBootstrapStyle style)
        {
            if (BorderStyles.TryGetValue(style, out var html))
            {
                return html;
            }

            return new TwBorderStyler();
        }

        /// <summary>
        /// Gets the foreground style for the given style.
        /// </summary>
        public static TwBorderStyler GetForegroundStyle(TwBootstrapStyle style)
        {
            if (ForegroundStyles.TryGetValue(style, out var html))
            {
                return html;
            }

            return new TwBorderStyler();
        }
    }
}
