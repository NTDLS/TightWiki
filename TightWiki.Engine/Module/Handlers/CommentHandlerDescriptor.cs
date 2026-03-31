using NTDLS.Helpers;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Module.Handlers;

namespace TightWiki.Engine.Module.Handlers
{
    /// <summary>
    /// Handles wiki comments. These are generally removed from the result.
    /// </summary>
    public class CommentHandlerDescriptor(ITwHandlerDescriptor descriptor)
        : HandlerDescriptor(descriptor.Plugin, descriptor.Method, descriptor.HandlerAttribute, descriptor.PluginAttribute), ITwCommentPlugin
    {

        /// <summary>
        /// Handles a wiki comment.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="text">The comment text</param>
        public async Task<TwPluginResult> Handle(ITwEngineState state, string text)
        {
            var result = (Task<TwPluginResult>)Method.Invoke(Plugin.Instance, [state, text]).EnsureNotNull();
            return await result;
        }
    }
}
