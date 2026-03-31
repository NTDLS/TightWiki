using NTDLS.Helpers;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Module.Handlers;

namespace TightWiki.Engine.Module.Handlers
{
    /// <summary>
    /// Handles wiki headings. These are automatically added to the table of contents.
    /// </summary>
    public class HeadingHandlerDescriptor(ITwHandlerDescriptor descriptor)
        : HandlerDescriptor(descriptor.Plugin, descriptor.Method, descriptor.HandlerAttribute, descriptor.PluginAttribute), ITwHeadingPlugin
    {

        /// <summary>
        /// Handles wiki headings. These are automatically added to the table of contents.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="depth">The size of the header, also used for table of table of contents indentation.</param>
        /// <param name="link">The self link reference.</param>
        /// <param name="text">The text for the self link.</param>
        public async Task<TwPluginResult> Handle(ITwEngineState state, int depth, string link, string text)
        {
            var result = (Task<TwPluginResult>)Method.Invoke(Plugin.Instance, [state, depth, link, text]).EnsureNotNull();
            return await result;
        }
    }
}
