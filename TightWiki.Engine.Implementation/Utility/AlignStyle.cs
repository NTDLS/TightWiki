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

        public static readonly Dictionary<string, AlignStyle> AlignStyles = new(StringComparer.OrdinalIgnoreCase)
        {
            { "start", new AlignStyle("text-start") },
            { "center", new AlignStyle("text-center") },
            { "end", new AlignStyle("text-end") },
        };


        public static AlignStyle GetStyle(string style)
        {
            if (AlignStyles.TryGetValue(style, out var html))
            {
                return html;
            }

            return new AlignStyle();
        }

    }
}
