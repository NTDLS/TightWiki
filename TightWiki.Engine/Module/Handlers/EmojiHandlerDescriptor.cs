using NTDLS.Helpers;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Module.Handlers;

namespace TightWiki.Engine.Module.Handlers
{
    /// <summary>
    /// Handles wiki emojis.
    /// </summary>
    public class EmojiHandlerDescriptor(ITwHandlerDescriptor descriptor)
        : HandlerDescriptor(descriptor.Plugin, descriptor.Method, descriptor.HandlerAttribute, descriptor.PluginAttribute), ITwEmojiPlugin
    {

        /// <summary>
        /// Handles an emoji instruction.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="key">The lookup key for the given emoji.</param>
        /// <param name="scale">The desired 1-100 scale factor for the emoji.</param>
        public async Task<TwPluginResult> Handle(ITwEngineState state, string key, int scale)
        {
            var result = (Task<TwPluginResult>)Method.Invoke(Plugin.Instance, [state, key, scale]).EnsureNotNull();
            return await result;
        }
    }
}
