using NTDLS.Helpers;
using System.Reflection;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Engine.Function;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Handlers;

namespace TightWiki.Plugin.Engine.Handlers
{
    /// <summary>
    /// Handles wiki headings. These are automatically added to the table of contents.
    /// </summary>
    public class TwHeadingHandlerDescriptor
        : TwEngineHandlerDescriptor, ITwHeadingHandler
    {
        public TwHeadingHandlerDescriptor(TwEnginePluginModule engineModule, MethodInfo method, ITwHandlerDescriptorAttribute attribute)
            : base(engineModule, method, attribute)
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
