using System.Text;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Styler;

namespace TightWiki.Plugin.Default.ScopeFunctions
{
    [TwPlugin("Layout & Containers", "Built-in scope functions.")]
    public class LayoutFunctions
    {
        [TwScopeFunctionPlugin("Card", "Renders a card with optional style and title.")]
        public async Task<TwPluginResult> Card(ITwEngineState state, string scopeBody,
            TwBootstrapStyle styleName = TwBootstrapStyle.Default, string? titleText = null)
        {
            var html = new StringBuilder();

            var borderStyle = TwBorderStyler.GetBorderStyle(styleName);
            var fillStyle = TwFillStyler.GetBackgroundStyle(styleName);

            html.Append($"<div class=\"card {borderStyle.ForegroundStyle} {borderStyle.BorderStyle} shadow-lg mb-3\">");
            if (string.IsNullOrEmpty(titleText) == false)
            {
                html.Append($"<div class=\"card-header {fillStyle.ForegroundStyle} {fillStyle.BackgroundStyle}\">{titleText}</div>");
            }
            html.Append("<div class=\"card-body\">");
            html.Append($"<p class=\"card-text\">{scopeBody}</p>");
            html.Append("</div>");
            html.Append("</div>");
            return new TwPluginResult(html.ToString());
        }

        [TwScopeFunctionPlugin("Jumbotron", "Renders a jumbotron with optional style and title.")]
        public async Task<TwPluginResult> Jumbotron(ITwEngineState state, string scopeBody,
            TwBootstrapStyle styleName = TwBootstrapStyle.Secondary, string? titleText = null)
        {
            var html = new StringBuilder();

            var fillStyle = TwFillStyler.GetBackgroundStyle(styleName);

            html.Append($"<div class=\"mt-4 p-5 {fillStyle.ForegroundStyle} {fillStyle.BackgroundStyle} rounded\">");
            if (!string.IsNullOrEmpty(titleText)) html.Append($"<h1>{titleText}</h1>");
            html.Append($"<p>{scopeBody}</p>");
            html.Append($"</div>");
            return new TwPluginResult(html.ToString());
        }
    }
}
