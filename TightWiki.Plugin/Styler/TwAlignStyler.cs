namespace TightWiki.Plugin.Styler
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

        public static readonly Dictionary<TwAlignStyle, TwAlignStyler> AlignStyles = new()
        {
            { TwAlignStyle.Default, new TwAlignStyler("") },
            { TwAlignStyle.Start, new TwAlignStyler("text-start") },
            { TwAlignStyle.Center, new TwAlignStyler("text-center") },
            { TwAlignStyle.End, new TwAlignStyler("text-end") },
        };

        public static TwAlignStyler GetStyle(TwAlignStyle style)
        {
            if (AlignStyles.TryGetValue(style, out var html))
            {
                return html;
            }

            return new TwAlignStyler();
        }

    }
}
