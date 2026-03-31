using NTDLS.Helpers;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Module.Handlers;

namespace TightWiki.Engine.Module.Handlers
{
    /// <summary>
    /// Handles wiki emojis.
    /// </summary>
    public class EmojiHandlerDescriptor
        : HandlerDescriptor, ITwEmojiHandler
    {
        public EmojiHandlerDescriptor(ITwHandlerDescriptor descriptor)
            : base(descriptor.EngineModule, descriptor.Method, descriptor.Attribute, descriptor.ModuleAttribute)
        {
        }

        /// <summary>
        /// Handles an emoji instruction.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="key">The lookup key for the given emoji.</param>
        /// <param name="scale">The desired 1-100 scale factor for the emoji.</param>
        public async Task<TwPluginResult> Handle(ITwEngineState state, string key, int scale)
        {
            var result = (Task<TwPluginResult>)Method.Invoke(EngineModule.Instance, [state, key, scale]).EnsureNotNull();
            return await result;
        }
    }
}
