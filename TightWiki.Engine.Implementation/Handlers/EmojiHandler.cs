using TightWiki.Plugin;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Handlers;

namespace TightWiki.Engine.Implementation.Handlers
{
    /// <summary>
    /// Handles wiki emojis.
    /// </summary>
    [TwPluginModule("Default emoji handler", "Handles wiki emojis.", 1000)]
    public class EmojiHandler
        : ITwEmojiHandler
    {
        /// <summary>
        /// Handles an emoji instruction.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="key">The lookup key for the given emoji.</param>
        /// <param name="scale">The desired 1-100 scale factor for the emoji.</param>
        [TwEmojiHandler("Default emoji handler", "Handles wiki emojis.")]
        public async Task<TwHandlerResult> Handle(ITwEngineState state, string key, int scale)
        {
            var emoji = state.Engine.WikiConfiguration.Emojis.FirstOrDefault(o => o.Shortcut == key);

            if (state.Engine.WikiConfiguration.Emojis.Exists(o => o.Shortcut == key))
            {
                if (scale != 100 && scale > 0 && scale <= 500)
                {
                    var emojiImage = $"<img src=\"{state.Engine.WikiConfiguration.BasePath}/file/Emoji/{key.Trim('%')}?Scale={scale}\" alt=\"{emoji?.Name}\" />";

                    return new TwHandlerResult(emojiImage);
                }
                else
                {
                    var emojiImage = $"<img src=\"{state.Engine.WikiConfiguration.BasePath}/file/Emoji/{key.Trim('%')}\" alt=\"{emoji?.Name}\" />";

                    return new TwHandlerResult(emojiImage);
                }
            }
            else
            {
                return new TwHandlerResult(key) { Instructions = [HandlerResultInstruction.DisallowNestedProcessing] };
            }
        }
    }
}
