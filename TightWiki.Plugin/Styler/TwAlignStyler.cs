namespace TightWiki.Plugin.Styler
{
    /// <summary>
    /// Alignment styler.
    /// </summary>
    public static class TwAlignStyler
    {
        private static readonly Dictionary<TwAlignStyle, string> AlignStyles = new()
        {
            { TwAlignStyle.Default, "" },
            { TwAlignStyle.Start, "text-start" },
            { TwAlignStyle.Center, "text-center" },
            { TwAlignStyle.End, "text-end" },
        };

        /// <summary>
        /// Gets the alignment style for the given style.
        /// </summary>
        public static string GetStyle(TwAlignStyle style)
        {
            if (AlignStyles.TryGetValue(style, out var html))
            {
                return html;
            }

            throw new Exception($"The given align style is not implemented: {style.ToString()}");
        }
    }
}
