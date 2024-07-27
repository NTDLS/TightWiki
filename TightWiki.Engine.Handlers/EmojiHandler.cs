using TightWiki.Configuration;
using TightWiki.Engine.Library;
using TightWiki.Engine.Library.Interfaces;
using static TightWiki.Engine.Library.Constants;

namespace TightWiki.Engine.Handlers
{
    public class EmojiHandler : IEmojiHandler
    {
        public HandlerResult Handle(IWikifier wikifier, string key, int scale)
        {
            var emoji = GlobalConfiguration.Emojis.FirstOrDefault(o => o.Shortcut == key);

            if (GlobalConfiguration.Emojis.Exists(o => o.Shortcut == key))
            {
                if (scale != 100 && scale > 0 && scale <= 500)
                {
                    var emojiImage = $"<img src=\"/file/Emoji/{key.Trim('%')}?Scale={scale}\" alt=\"{emoji?.Name}\" />";

                    return new HandlerResult(emojiImage) { Instructions = [HandlerResultInstruction.TruncateTrailingLine] };
                    //var identifier = StoreMatch(WikiMatchType.Variable, pageContent, match.Value, emojiImage);
                    //pageContent.Replace($"{identifier}\n", $"{identifier}"); //Kill trailing newline.
                }
                else
                {
                    var emojiImage = $"<img src=\"/file/Emoji/{key.Trim('%')}\" alt=\"{emoji?.Name}\" />";

                    return new HandlerResult(emojiImage) { Instructions = [HandlerResultInstruction.TruncateTrailingLine] };

                    //var identifier = StoreMatch(WikiMatchType.Variable, pageContent, match.Value, emojiImage);
                    //pageContent.Replace($"{identifier}\n", $"{identifier}"); //Kill trailing newline.
                }
            }
            else
            {
                return new HandlerResult(key) { Instructions = [HandlerResultInstruction.TruncateTrailingLine, HandlerResultInstruction.DisallowNestedProcessing] };
                //var identifier = StoreMatch(WikiMatchType.Variable, pageContent, match.Value, match.Value, false);
                //pageContent.Replace($"{identifier}\n", $"{identifier}"); //Kill trailing newline.
            }

            return new HandlerResult() { Instructions = [HandlerResultInstruction.TruncateTrailingLine] };
        }
    }
}
