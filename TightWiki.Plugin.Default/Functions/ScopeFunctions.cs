using System.Net;
using System.Text;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Styler;

namespace TightWiki.Plugin.Default.Functions
{
    [TwPluginModule("Processing Instructions Functions", "Built-in scope functions.", 1000)]
    public class ScopeFunctions
        : ITwDisabiguation
    {
        [TwScopeFunction("Code", "Renders a block of code with optional syntax highlighting.", true)]
        public async Task<TwPluginResult> Code(ITwEngineState state, string scopeBody,
            TightWikiCodeLanguage codeLanguage = TightWikiCodeLanguage.Auto)
        {
            var html = new StringBuilder();

            var wikiScopeBody = new TwString(scopeBody.Replace("\r\n", "\n").Replace("\t", "    "));

            // On the off-chance that the scope body contains matches that need to be swapped
            //  in, we need to swap them in before encoding the text for HTML. This is to allow us
            //  to display wiki code and also use literals #{}# within code blocks.
            state.SwapInStoredMatches(wikiScopeBody, true);
            state.SwapInLineBreaks(wikiScopeBody, "\r\n");

            var encodedScopeBody = WebUtility.HtmlEncode(wikiScopeBody.ToString());

            if (codeLanguage == TightWikiCodeLanguage.Auto)
            {
                html.Append($"<pre><code>{encodedScopeBody}</code></pre>");
            }
            else
            {
                html.Append($"<pre class=\"language-{codeLanguage.ToString().ToLowerInvariant()}\"><code>{encodedScopeBody}</code></pre>");
            }

            return new TwPluginResult(html.ToString())
            {
                Instructions = [HandlerResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwScopeFunction("Table", "Renders a table with optional border and header row.")]
        public async Task<TwPluginResult> Table(ITwEngineState state, string scopeBody,
            bool hasBorder = true, bool isFirstRowHeader = true)
            => await BaseTable(state, scopeBody, hasBorder, isFirstRowHeader);

        [TwScopeFunction("StripedTable", "Renders a striped table with optional border and header row.")]
        public async Task<TwPluginResult> StripedTable(ITwEngineState state, string scopeBody,
            bool hasBorder = true, bool isFirstRowHeader = true)
            => await BaseTable(state, scopeBody, hasBorder, isFirstRowHeader);

        private async Task<TwPluginResult> BaseTable(ITwEngineState state, string scopeBody,
            bool hasBorder = true, bool isFirstRowHeader = true, bool isStriped = false)
        {
            var html = new StringBuilder();

            html.Append($"<table class=\"table");

            if (isStriped)
            {
                html.Append(" table-striped");
            }
            if (hasBorder)
            {
                html.Append(" table-bordered");
            }

            html.Append($"\">");

            var lines = scopeBody.Split(['\n'], StringSplitOptions.RemoveEmptyEntries).Select(o => o.Trim()).Where(o => o.Length > 0);

            int rowNumber = 0;

            foreach (var lineText in lines)
            {
                var columns = lineText.Split("||");

                if (rowNumber == 0 && isFirstRowHeader)
                {
                    html.Append($"<thead>");
                }
                else if (rowNumber == 1 && isFirstRowHeader || rowNumber == 0 && isFirstRowHeader == false)
                {
                    html.Append($"<tbody>");
                }

                html.Append($"<tr>");
                foreach (var columnText in columns)
                {
                    if (rowNumber == 0 && isFirstRowHeader)
                    {
                        html.Append($"<td><strong>{columnText}</strong></td>");
                    }
                    else
                    {
                        html.Append($"<td>{columnText}</td>");
                    }
                }

                if (rowNumber == 0 && isFirstRowHeader)
                {
                    html.Append($"</thead>");
                }
                html.Append($"</tr>");

                rowNumber++;
            }

            html.Append($"</tbody>");
            html.Append($"</table>");

            return new TwPluginResult(html.ToString());
        }

        [TwScopeFunction("Bullets", "Renders a list of bullets with optional nesting.")]
        public async Task<TwPluginResult> Bullets(ITwEngineState state, string scopeBody,
            TightWikiBulletStyle type = TightWikiBulletStyle.Unordered)
        {
            var html = new StringBuilder();

            switch (type)
            {
                case TightWikiBulletStyle.Unordered:
                    {
                        var lines = scopeBody.Split(['\n'], StringSplitOptions.RemoveEmptyEntries).Select(o => o.Trim()).Where(o => o.Length > 0);

                        int currentLevel = 0;

                        foreach (var line in lines)
                        {
                            int newIndent = 0;
                            for (; newIndent < line.Length && line[newIndent] == '>'; newIndent++)
                            {
                                //Count how many '>' are at the start of the line.
                            }
                            newIndent++;

                            if (newIndent < currentLevel)
                            {
                                for (; currentLevel != newIndent; currentLevel--)
                                {
                                    html.Append($"</ul>");
                                }
                            }
                            else if (newIndent > currentLevel)
                            {
                                for (; currentLevel != newIndent; currentLevel++)
                                {
                                    html.Append($"<ul>");
                                }
                            }

                            html.Append($"<li>{line.Trim(['>'])}</li>");
                        }

                        for (; currentLevel > 0; currentLevel--)
                        {
                            html.Append($"</ul>");
                        }
                    }
                    break;
                case TightWikiBulletStyle.Ordered:
                    {
                        var lines = scopeBody.Split(['\n'], StringSplitOptions.RemoveEmptyEntries).Select(o => o.Trim()).Where(o => o.Length > 0);

                        int currentLevel = 0;

                        foreach (var line in lines)
                        {
                            int newIndent = 0;
                            for (; newIndent < line.Length && line[newIndent] == '>'; newIndent++)
                            {
                                //Count how many '>' are at the start of the line.
                            }
                            newIndent++;

                            if (newIndent < currentLevel)
                            {
                                for (; currentLevel != newIndent; currentLevel--)
                                {
                                    html.Append($"</ol>");
                                }
                            }
                            else if (newIndent > currentLevel)
                            {
                                for (; currentLevel != newIndent; currentLevel++)
                                {
                                    html.Append($"<ol>");
                                }
                            }

                            html.Append($"<li>{line.Trim(['>'])}</li>");
                        }

                        for (; currentLevel > 0; currentLevel--)
                        {
                            html.Append($"</ol>");
                        }
                    }
                    break;
            }
            return new TwPluginResult(html.ToString());
        }

        [TwScopeFunction("DefineSnippet", "Defines a reusable snippet of content.")]
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

        [TwScopeFunction("Alert", "Renders an alert box with optional style and title.")]
        public async Task<TwPluginResult> Alert(ITwEngineState state, string scopeBody,
            TightWikiBootstrapStyle styleName = TightWikiBootstrapStyle.Default, string titleText = "")
        {
            var html = new StringBuilder();

            var style = styleName == TightWikiBootstrapStyle.Default ? "" : $"alert-{styleName.ToString().ToLowerInvariant()}";

            if (!string.IsNullOrEmpty(titleText)) scopeBody = $"<h3>{titleText}</h3>{scopeBody}";
            html.Append($"<div class=\"alert {style} shadow-lg\">{scopeBody}</div>");
            return new TwPluginResult(html.ToString());
        }

        [TwScopeFunction("Order", "Orders a list of items in ascending or descending order.")]
        public async Task<TwPluginResult> Order(ITwEngineState state, string scopeBody,
            TightWikiOrder direction = TightWikiOrder.Ascending)
        {
            var html = new StringBuilder();

            var lines = scopeBody.Split("\n").Select(o => o.Trim()).ToList();

            switch (direction)
            {
                case TightWikiOrder.Ascending:
                    html.Append(string.Join("\r\n", lines.OrderBy(o => o)));
                    break;
                case TightWikiOrder.Descending:
                    html.Append(string.Join("\r\n", lines.OrderByDescending(o => o)));
                    break;
            }
            return new TwPluginResult(html.ToString());
        }

        [TwScopeFunction("Jumbotron", "Renders a jumbotron with optional style and title.")]
        public async Task<TwPluginResult> Jumbotron(ITwEngineState state, string scopeBody,
            TightWikiBootstrapStyle styleName = TightWikiBootstrapStyle.Secondary, string? titleText = null)
        {
            var html = new StringBuilder();

            var fillStyle = TwFillStyler.GetBackgroundStyle(styleName);

            html.Append($"<div class=\"mt-4 p-5 {fillStyle.ForegroundStyle} {fillStyle.BackgroundStyle} rounded\">");
            if (!string.IsNullOrEmpty(titleText)) html.Append($"<h1>{titleText}</h1>");
            html.Append($"<p>{scopeBody}</p>");
            html.Append($"</div>");
            return new TwPluginResult(html.ToString());
        }

        [TwScopeFunction("Foreground", "Renders text with a specified foreground color.")]
        public async Task<TwPluginResult> Foreground(ITwEngineState state, string scopeBody,
            TightWikiBootstrapStyle styleName = TightWikiBootstrapStyle.Default)
        {
            var html = new StringBuilder();

            var style = TwFillStyler.GetForegroundStyle(styleName).Swap();
            html.Append($"<p class=\"{style.ForegroundStyle} {style.BackgroundStyle}\">{scopeBody}</p>");
            return new TwPluginResult(html.ToString());
        }

        [TwScopeFunction("Background", "Renders text with a specified background color.")]
        public async Task<TwPluginResult> Background(ITwEngineState state, string scopeBody,
            TightWikiBootstrapStyle styleName = TightWikiBootstrapStyle.Default)
        {
            var html = new StringBuilder();

            var style = TwFillStyler.GetBackgroundStyle(styleName).Swap();
            html.Append($"<div class=\"p-3 mb-2 {style.ForegroundStyle} {style.BackgroundStyle}\">{scopeBody}</div>");
            return new TwPluginResult(html.ToString());
        }

        [TwScopeFunction("Collapse", "Renders a collapsible section with optional link text.")]
        public async Task<TwPluginResult> Collapse(ITwEngineState state, string scopeBody, string linkText = "Show")
        {
            var html = new StringBuilder();

            string uid = "A" + Guid.NewGuid().ToString().Replace("-", "");
            html.Append($"<a data-bs-toggle=\"collapse\" href=\"#{uid}\" role=\"button\" aria-expanded=\"false\" aria-controls=\"{uid}\">{linkText}</a>");
            html.Append($"<div class=\"collapse\" id=\"{uid}\">");
            html.Append($"<div class=\"card card-body\"><p class=\"card-text\">{scopeBody}</p></div></div>");
            return new TwPluginResult(html.ToString());
        }

        [TwScopeFunction("Callout", "Renders a callout box with optional style and title.")]
        public async Task<TwPluginResult> Callout(ITwEngineState state, string scopeBody,
            TightWikiBootstrapStyle styleName = TightWikiBootstrapStyle.Default, string? titleText = null)
        {
            var html = new StringBuilder();

            html.Append($"<div class=\"bd-callout bd-callout-{styleName.ToString().ToLowerInvariant()} shadow-lg\">");
            if (!string.IsNullOrWhiteSpace(titleText)) html.Append($"<h4>{titleText}</h4>");
            html.Append($"{scopeBody}");
            html.Append($"</div>");
            return new TwPluginResult(html.ToString());
        }

        [TwScopeFunction("Card", "Renders a card with optional style and title.")]
        public async Task<TwPluginResult> Card(ITwEngineState state, string scopeBody,
            TightWikiBootstrapStyle styleName = TightWikiBootstrapStyle.Default, string? titleText = null)
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

        [TwScopeFunction("BlockQuote", "Renders a blockquote with optional alignment and caption.")]
        public async Task<TwPluginResult> BlockQuote(ITwEngineState state, string scopeBody,
            TightWikiAlignStyle styleName = TightWikiAlignStyle.Start, string? caption = null)
        {
            var html = new StringBuilder();

            var style = TwAlignStyler.GetStyle(styleName);

            html.Append($"<figure class=\"{style.Style}\">");
            html.Append($"<blockquote class=\"blockquote\">{scopeBody}</blockquote >");

            if (string.IsNullOrEmpty(caption) == false)
            {
                html.Append("<figcaption class=\"blockquote-footer\">");
                html.Append($"{caption}");
                html.Append("</figcaption>");
            }
            html.Append("</figure>");
            return new TwPluginResult(html.ToString());
        }

        [TwScopeFunction("Figure", "Renders a figure with optional alignment and caption.")]
        public async Task<TwPluginResult> Figure(ITwEngineState state, string scopeBody,
            TightWikiAlignStyle styleName = TightWikiAlignStyle.Default, string? caption = null)
        {
            var html = new StringBuilder();

            var style = TwAlignStyler.GetStyle(styleName);

            html.Append($"<figure class=\"figure\">");
            html.Append($"{scopeBody}");

            if (string.IsNullOrEmpty(caption) == false)
            {
                html.Append($"<figcaption class=\"figure-caption {style.Style}\">");
                html.Append($"{caption}");
                html.Append("</figcaption>");
            }
            html.Append("</figure>");
            return new TwPluginResult(html.ToString());
        }
    }
}
