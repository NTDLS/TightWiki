namespace TightWiki.Engine.Implementation.Utility
{
    public class AlignStyle
    {
        public string Style { get; set; } = String.Empty;

        public AlignStyle(string style)
        {
            Style = style;
        }

        public AlignStyle()
        {
        }

        public static readonly Dictionary<TightWikiAlignStyle, AlignStyle> AlignStyles = new()
        {
            { TightWikiAlignStyle.Default, new AlignStyle("") },
            { TightWikiAlignStyle.Start, new AlignStyle("text-start") },
            { TightWikiAlignStyle.Center, new AlignStyle("text-center") },
            { TightWikiAlignStyle.End, new AlignStyle("text-end") },
        };

        public static AlignStyle GetStyle(TightWikiAlignStyle style)
        {
            if (AlignStyles.TryGetValue(style, out var html))
            {
                return html;
            }

            return new AlignStyle();
        }

    }
}
