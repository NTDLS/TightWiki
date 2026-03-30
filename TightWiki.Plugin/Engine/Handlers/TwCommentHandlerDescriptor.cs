using NTDLS.Helpers;
using TightWiki.Plugin.Engine.Function;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Handlers;

namespace TightWiki.Plugin.Engine.Handlers
{
    /// <summary>
    /// Handles wiki comments. These are generally removed from the result.
    /// </summary>
    public class TwCommentHandlerDescriptor
        : TwEngineHandlerDescriptor, ITwCommentHandler
    {
        public TwCommentHandlerDescriptor(TwEngineHandlerDescriptor descriptor)
            : base(descriptor.EngineModule, descriptor.Method, descriptor.Attribute, descriptor.ModuleAttribute)
        {
        }

        /// <summary>
        /// Handles a wiki comment.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="text">The comment text</param>
        public async Task<TwHandlerResult> Handle(ITwEngineState state, string text)
        {
            var result = (Task<TwHandlerResult>)Method.Invoke(EngineModule.Instance, [state, text]).EnsureNotNull();
            return await result;
        }
    }
}
