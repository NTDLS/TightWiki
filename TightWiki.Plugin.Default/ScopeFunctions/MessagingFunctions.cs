using System.Text;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;

namespace TightWiki.Plugin.Default.ScopeFunctions
{
    [TwPlugin("Alerts & Emphasis", "Built-in scope functions.")]
    public class MessagingFunctions
    {
        [TwScopeFunctionPlugin("Alert", "Renders an alert box with optional style and title.")]
        public async Task<TwPluginResult> Alert(ITwEngineState state, string scopeBody,
            TwBootstrapStyle styleName = TwBootstrapStyle.Default, string titleText = "")
        {
            var html = new StringBuilder();

            var style = styleName == TwBootstrapStyle.Default ? "" : $"alert-{styleName.ToString().ToLowerInvariant()}";

            if (!string.IsNullOrEmpty(titleText)) scopeBody = $"<h3>{titleText}</h3>{scopeBody}";
            html.Append($"<div class=\"alert {style} shadow-lg\">{scopeBody}</div>");
            return new TwPluginResult(html.ToString());
        }

        [TwScopeFunctionPlugin("Callout", "Renders a callout box with optional style and title.")]
        public async Task<TwPluginResult> Callout(ITwEngineState state, string scopeBody,
            TwBootstrapStyle styleName = TwBootstrapStyle.Default, string? titleText = null)
        {
            var html = new StringBuilder();

            html.Append($"<div class=\"bd-callout bd-callout-{styleName.ToString().ToLowerInvariant()} shadow-lg\">");
            if (!string.IsNullOrWhiteSpace(titleText)) html.Append($"<h4>{titleText}</h4>");
            html.Append($"{scopeBody}");
            html.Append($"</div>");
            return new TwPluginResult(html.ToString());
        }

        [TwScopeFunctionPlugin("Collapse", "Renders a collapsible section with optional link text.")]
        public async Task<TwPluginResult> Collapse(ITwEngineState state, string scopeBody, string linkText = "Show")
        {
            var html = new StringBuilder();

            string link = state.GetNextTagMarker("Collapse");
            html.Append($"<a data-bs-toggle=\"collapse\" href=\"#{link}\" role=\"button\" aria-expanded=\"false\" aria-controls=\"{link}\">{linkText}</a>");
            html.Append($"<div class=\"collapse\" id=\"{link}\">");
            html.Append($"<div class=\"card card-body\"><p class=\"card-text\">{scopeBody}</p></div></div>");
            return new TwPluginResult(html.ToString());
        }
    }
}
