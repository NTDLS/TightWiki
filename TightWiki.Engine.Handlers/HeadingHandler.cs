using TightWiki.Engine.Library;
using TightWiki.Engine.Library.Interfaces;
using static TightWiki.Engine.Library.Constants;

namespace TightWiki.Engine.Handlers
{
    public class HeadingHandler : IHeadingHandler
    {
        public HandlerResult Handle(IWikifier wikifier, int depth,  string link, string text)
        {
            if (depth >= 2 && depth <= 6)
            {
                int fontSize = 8 - depth;
                if (fontSize < 5) fontSize = 5;

                string html = "<font size=\"" + fontSize + "\"><a name=\"" + link + "\"><span class=\"WikiH" + (depth - 1).ToString() + "\">" + text + "</span></a></font>\r\n";
                return new HandlerResult(html);
            }

            return new HandlerResult() { Instructions = [HandlerResultInstruction.Skip] };
        }
    }
}
