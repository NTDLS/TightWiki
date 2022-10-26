using System;

namespace TightWiki.Shared.Wiki
{
    public class CardStyle
    {
        public string ForegroundStyle { get; set; } = String.Empty;
        public string BackgroundStyle { get; set; } = String.Empty;

        public CardStyle(string foregroundStyle, string backgroundStyle)
        {
            ForegroundStyle = foregroundStyle;
            BackgroundStyle = backgroundStyle;
        }

        public CardStyle()
        {

        }
    }
}
