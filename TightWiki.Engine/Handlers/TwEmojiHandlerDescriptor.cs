using NTDLS.Helpers;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Engine.Function;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Handlers;

namespace TightWiki.Engine.Handlers
{
    /// <summary>
    /// Handles wiki emojis.
    /// </summary>
    public class TwEmojiHandlerDescriptor
        : TwEngineHandlerDescriptor, ITwEmojiHandler
    {
        public TwEmojiHandlerDescriptor(TwEngineHandlerDescriptor descriptor)
            : base(descriptor.EngineModule, descriptor.Method, descriptor.Attribute, descriptor.ModuleAttribute)
        {
        }

        /// <summary>
        /// Handles an emoji instruction.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="key">The lookup key for the given emoji.</param>
        /// <param name="scale">The desired 1-100 scale factor for the emoji.</param>
        public async Task<TwHandlerResult> Handle(ITwEngineState state, string key, int scale)
        {
            var result = (Task<TwHandlerResult>)Method.Invoke(EngineModule.Instance, [state, key, scale]).EnsureNotNull();
            return await result;
        }
    }
}
