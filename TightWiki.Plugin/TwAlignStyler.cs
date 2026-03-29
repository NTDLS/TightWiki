namespace TightWiki.Plugin
{
    public class TwAlignStyler
    {
        public string Style { get; set; } = String.Empty;

        public TwAlignStyler(string style)
        {
            Style = style;
        }

        public TwAlignStyler()
        {
        }

        public static readonly Dictionary<TightWikiAlignStyle, TwAlignStyler> AlignStyles = new()
        {
            { TightWikiAlignStyle.Default, new TwAlignStyler("") },
            { TightWikiAlignStyle.Start, new TwAlignStyler("text-start") },
            { TightWikiAlignStyle.Center, new TwAlignStyler("text-center") },
            { TightWikiAlignStyle.End, new TwAlignStyler("text-end") },
        };

        public static TwAlignStyler GetStyle(TightWikiAlignStyle style)
        {
            if (AlignStyles.TryGetValue(style, out var html))
            {
                return html;
            }

            return new TwAlignStyler();
        }

    }
}
