using System.Text;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Models;

namespace TightWiki.Plugin.Default.StandardFunctions
{
    [TwPlugin("Formatting & Rendering", "Built-in standard functions.")]
    public class FormattingFunctions
    {
        [TwStandardFunctionPlugin("Color", "Applies a color to the given text.")]
        public async Task<TwPluginResult> Color(ITwEngineState state, string color, string text)
        {
            return new TwPluginResult($"<font color=\"{color}\">{text}</font>");
        }

        [TwStandardFunctionPlugin("BR", "Inserts a line break into the page.")]
        public async Task<TwPluginResult> BR(ITwEngineState state, int count = 1) => await NewLine(state, count);

        [TwStandardFunctionPlugin("NL", "Inserts a line break into the page.")]
        public async Task<TwPluginResult> NL(ITwEngineState state, int count = 1) => await NewLine(state, count);

        [TwStandardFunctionPlugin("NewLine", "Inserts a line break into the page.")]
        public async Task<TwPluginResult> NewLine(ITwEngineState state, int count = 1) //##NewLine([optional:default=1]count)
        {
            var lineBreaks = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                lineBreaks.Append("<br />");
            }
            return new TwPluginResult(lineBreaks.ToString());
        }

        [TwStandardFunctionPlugin("HR", "Inserts a horizontal rule into the page.")]
        public async Task<TwPluginResult> HR(ITwEngineState state, int height = 1)
        {
            return new TwPluginResult($"<hr class=\"my-{height}\">");
        }
    }
}
