using System.Text;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;

namespace TightWiki.Plugin.Default.ScopeFunctions
{
    [TwPlugin("Content Reuse & Transformation", "Built-in scope functions.")]
    public class ContentFunctions
    {
        [TwScopeFunctionPlugin("DefineSnippet", "Defines a reusable snippet of content.")]
        public async Task<TwPluginResult> DefineSnippet(ITwEngineState state, string scopeBody,
            string name)
        {
            var html = new StringBuilder();

            if (!state.Snippets.TryAdd(name, scopeBody))
            {
                state.Snippets[name] = scopeBody;
            }

            return new TwPluginResult(html.ToString());
        }

        [TwScopeFunctionPlugin("Order", "Orders a list of items in ascending or descending order.")]
        public async Task<TwPluginResult> Order(ITwEngineState state, string scopeBody,
            TwOrder direction = TwOrder.Ascending)
        {
            var html = new StringBuilder();

            var lines = scopeBody.Split("\n").Select(o => o.Trim()).ToList();

            switch (direction)
            {
                case TwOrder.Ascending:
                    html.Append(string.Join("\r\n", lines.OrderBy(o => o)));
                    break;
                case TwOrder.Descending:
                    html.Append(string.Join("\r\n", lines.OrderByDescending(o => o)));
                    break;
            }
            return new TwPluginResult(html.ToString());
        }
    }
}
