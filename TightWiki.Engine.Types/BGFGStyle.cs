namespace TightWiki.Engine.Types
{
    public class BGFGStyle
    {
        public string ForegroundStyle { get; set; } = String.Empty;
        public string BackgroundStyle { get; set; } = String.Empty;

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
    }
}
