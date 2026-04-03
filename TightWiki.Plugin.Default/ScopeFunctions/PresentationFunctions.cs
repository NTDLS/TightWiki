using System.Net;
using System.Text;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Styler;

namespace TightWiki.Plugin.Default.ScopeFunctions
{
    [TwPlugin("Presentation Functions", "Built-in scope functions.")]
    public class PresentationFunctions
    {
        [TwScopeFunctionPlugin("Code", "Renders a block of code with optional syntax highlighting.", isFirstChance: true)]
        public async Task<TwPluginResult> Code(ITwEngineState state, string scopeBody,
            TwCodeLanguage codeLanguage = TwCodeLanguage.Auto)
        {
            var html = new StringBuilder();

            var wikiScopeBody = new TwString(scopeBody.Replace("\r\n", "\n").Replace("\t", "    "));

            // On the off-chance that the scope body contains matches that need to be swapped
            //  in, we need to swap them in before encoding the text for HTML. This is to allow us
            //  to display wiki code and also use literals #{}# within code blocks.
            state.SwapInStoredMatches(wikiScopeBody, true);
            state.SwapInLineBreaks(wikiScopeBody, "\r\n");

            var encodedScopeBody = WebUtility.HtmlEncode(wikiScopeBody.ToString());

            if (codeLanguage == TwCodeLanguage.Auto)
            {
                html.Append($"<pre><code>{encodedScopeBody}</code></pre>");
            }
            else
            {
                html.Append($"<pre class=\"language-{codeLanguage.ToString().ToLowerInvariant()}\"><code>{encodedScopeBody}</code></pre>");
            }

            return new TwPluginResult(html.ToString())
            {
                Instructions = [TwResultInstruction.DisallowNestedProcessing]
            };
        }

        [TwScopeFunctionPlugin("Table", "Renders a table with optional border and header row.")]
        public async Task<TwPluginResult> Table(ITwEngineState state, string scopeBody,
            bool hasBorder = true, bool isFirstRowHeader = true)
            => await BaseTable(state, scopeBody, hasBorder, isFirstRowHeader);

        [TwScopeFunctionPlugin("StripedTable", "Renders a striped table with optional border and header row.")]
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

        [TwScopeFunctionPlugin("Bullets", "Renders a list of bullets with optional nesting.")]
        public async Task<TwPluginResult> Bullets(ITwEngineState state, string scopeBody,
            TwBulletStyle type = TwBulletStyle.Unordered)
        {
            var html = new StringBuilder();

            switch (type)
            {
                case TwBulletStyle.Unordered:
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
                case TwBulletStyle.Ordered:
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

        [TwScopeFunctionPlugin("BlockQuote", "Renders a blockquote with optional alignment and caption.")]
        public async Task<TwPluginResult> BlockQuote(ITwEngineState state, string scopeBody,
            TwAlignStyle styleName = TwAlignStyle.Start, string? caption = null)
        {
            var html = new StringBuilder();

            var align = TwAlignStyler.GetStyle(styleName);

            html.Append($"<figure class=\"{align}\">");
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

        [TwScopeFunctionPlugin("Figure", "Renders a figure with optional alignment and caption.")]
        public async Task<TwPluginResult> Figure(ITwEngineState state, string scopeBody,
            TwAlignStyle styleName = TwAlignStyle.Default, string? caption = null)
        {
            var html = new StringBuilder();

            var align = TwAlignStyler.GetStyle(styleName);

            html.Append($"<figure class=\"figure\">");
            html.Append($"{scopeBody}");

            if (string.IsNullOrEmpty(caption) == false)
            {
                html.Append($"<figcaption class=\"figure-caption {align}\">");
                html.Append($"{caption}");
                html.Append("</figcaption>");
            }
            html.Append("</figure>");
            return new TwPluginResult(html.ToString());
        }
    }
}
