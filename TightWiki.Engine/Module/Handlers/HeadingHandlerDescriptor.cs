using NTDLS.Helpers;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Module.Handlers;

namespace TightWiki.Engine.Module.Handlers
{
    /// <summary>
    /// Handles wiki headings. These are automatically added to the table of contents.
    /// </summary>
    public class HeadingHandlerDescriptor
        : HandlerDescriptor, ITwHeadingHandler
    {
        public HeadingHandlerDescriptor(ITwHandlerDescriptor descriptor)
            : base(descriptor.EngineModule, descriptor.Method, descriptor.Attribute, descriptor.ModuleAttribute)
        {
        }

        /// <summary>
        /// Handles wiki headings. These are automatically added to the table of contents.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="depth">The size of the header, also used for table of table of contents indentation.</param>
        /// <param name="link">The self link reference.</param>
        /// <param name="text">The text for the self link.</param>
        public async Task<TwHandlerResult> Handle(ITwEngineState state, int depth, string link, string text)
        {
            var result = (Task<TwHandlerResult>)Method.Invoke(EngineModule.Instance, [state, depth, link, text]).EnsureNotNull();
            return await result;
        }
    }
}
