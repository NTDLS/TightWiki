using TightWiki.Engine.Library;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.Models;
using static TightWiki.Engine.Library.Constants;

namespace TightWiki.Engine.Implementation
{
    /// <summary>
    /// Handles wiki emojis.
    /// </summary>
    public class EmojiHandler : IEmojiHandler
    {
        /// <summary>
        /// Handles an emoji instruction.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="key">The lookup key for the given emoji.</param>
        /// <param name="scale">The desired 1-100 scale factor for the emoji.</param>
        public HandlerResult Handle(ITightEngineState state, string key, int scale)
        {
            var emoji = GlobalConfiguration.Emojis.FirstOrDefault(o => o.Shortcut == key);

            if (GlobalConfiguration.Emojis.Exists(o => o.Shortcut == key))
            {
                if (scale != 100 && scale > 0 && scale <= 500)
                {
                    var emojiImage = $"<img src=\"/file/Emoji/{key.Trim('%')}?Scale={scale}\" alt=\"{emoji?.Name}\" />";

                    return new HandlerResult(emojiImage);
                }
                else
                {
                    var emojiImage = $"<img src=\"/file/Emoji/{key.Trim('%')}\" alt=\"{emoji?.Name}\" />";

                    return new HandlerResult(emojiImage);
                }
            }
            else
            {
                return new HandlerResult(key) { Instructions = [HandlerResultInstruction.DisallowNestedProcessing] };
            }
        }
    }
}
