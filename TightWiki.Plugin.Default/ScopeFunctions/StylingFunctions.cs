using System.Text;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Styler;

namespace TightWiki.Plugin.Default.ScopeFunctions
{
    [TwPlugin("Color & Visual Styling", "Built-in scope functions.")]
    public class StylingFunctions
    {
        [TwScopeFunctionPlugin("Foreground", "Renders text with a specified foreground color.")]
        public async Task<TwPluginResult> Foreground(ITwEngineState state, string scopeBody,
            TwBootstrapStyle styleName = TwBootstrapStyle.Default)
        {
            var html = new StringBuilder();

            var style = TwFillStyler.GetForegroundStyle(styleName).Swap();
            html.Append($"<p class=\"{style.ForegroundStyle} {style.BackgroundStyle}\">{scopeBody}</p>");
            return new TwPluginResult(html.ToString());
        }

        [TwScopeFunctionPlugin("Background", "Renders text with a specified background color.")]
        public async Task<TwPluginResult> Background(ITwEngineState state, string scopeBody,
            TwBootstrapStyle styleName = TwBootstrapStyle.Default)
        {
            var html = new StringBuilder();

            var style = TwFillStyler.GetBackgroundStyle(styleName).Swap();
            html.Append($"<div class=\"p-3 mb-2 {style.ForegroundStyle} {style.BackgroundStyle}\">{scopeBody}</div>");
            return new TwPluginResult(html.ToString());
        }
    }
}
