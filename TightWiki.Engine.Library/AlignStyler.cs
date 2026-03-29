using TightWiki.Library;

namespace TightWiki.Engine.Library
{
    public class AlignStyler
    {
        public string Style { get; set; } = String.Empty;

        public AlignStyler(string style)
        {
            Style = style;
        }

        public AlignStyler()
        {
        }

        public static readonly Dictionary<TightWikiAlignStyle, AlignStyler> AlignStyles = new()
        {
            { TightWikiAlignStyle.Default, new AlignStyler("") },
            { TightWikiAlignStyle.Start, new AlignStyler("text-start") },
            { TightWikiAlignStyle.Center, new AlignStyler("text-center") },
            { TightWikiAlignStyle.End, new AlignStyler("text-end") },
        };

        public static AlignStyler GetStyle(TightWikiAlignStyle style)
        {
            if (AlignStyles.TryGetValue(style, out var html))
            {
                return html;
            }

            return new AlignStyler();
        }

    }
}
