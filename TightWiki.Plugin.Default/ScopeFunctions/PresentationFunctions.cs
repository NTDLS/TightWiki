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
            => await BaseTable(state, scopeBody, hasBorder, isFirstRowHeader, isStriped: true);

        /// <summary>
        /// A cell containing only this marker is merged into the cell to its left (colspan).
        /// </summary>
        private const string MergeLeftMarker = "<";

        /// <summary>
        /// A cell containing only this marker is merged into the cell above it (rowspan).
        /// </summary>
        private const string MergeUpMarker = "^";

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

            var rows = scopeBody.Split(['\n'], StringSplitOptions.RemoveEmptyEntries)
                .Select(o => o.Trim()).Where(o => o.Length > 0)
                .Select(o => o.Split("||")).ToList();

            var anchors = ResolveMergedCells(rows, isFirstRowHeader);

            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                bool isHeaderRow = rowIndex == 0 && isFirstRowHeader;

                if (isHeaderRow)
                {
                    html.Append($"<thead>");
                }
                else if (rowIndex == 0 || (rowIndex == 1 && isFirstRowHeader))
                {
                    html.Append($"<tbody>");
                }

                html.Append($"<tr>");
                for (int columnIndex = 0; columnIndex < rows[rowIndex].Length; columnIndex++)
                {
                    if (!anchors.TryGetValue((rowIndex, columnIndex), out var span))
                    {
                        continue; //This cell was merged into another cell.
                    }

                    var columnText = rows[rowIndex][columnIndex];

                    html.Append("<td");
                    if (span.ColSpan > 1)
                    {
                        html.Append($" colspan=\"{span.ColSpan}\"");
                    }
                    if (span.RowSpan > 1)
                    {
                        html.Append($" rowspan=\"{span.RowSpan}\"");
                    }
                    html.Append('>');

                    if (isHeaderRow)
                    {
                        html.Append($"<strong>{columnText}</strong>");
                    }
                    else
                    {
                        html.Append(columnText);
                    }
                    html.Append("</td>");
                }
                html.Append($"</tr>");

                if (isHeaderRow)
                {
                    html.Append($"</thead>");
                }
            }

            if (rows.Count > (isFirstRowHeader ? 1 : 0))
            {
                html.Append($"</tbody>");
            }
            html.Append($"</table>");

            return new TwPluginResult(html.ToString());
        }

        /// <summary>
        /// Resolves the merge markers in a table, returning the span of every cell that is rendered.
        /// Cells that were merged into another cell are absent from the result.
        /// </summary>
        private static Dictionary<(int Row, int Column), (int ColSpan, int RowSpan)> ResolveMergedCells(
            List<string[]> rows, bool isFirstRowHeader)
        {
            //The cell that each cell belongs to; unmerged cells belong to themselves.
            var owners = new Dictionary<(int Row, int Column), (int Row, int Column)>();

            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                for (int columnIndex = 0; columnIndex < rows[rowIndex].Length; columnIndex++)
                {
                    var marker = rows[rowIndex][columnIndex].Trim();

                    if (marker == MergeLeftMarker)
                    {
                        if (columnIndex == 0)
                        {
                            throw new Exception($"Table row {rowIndex + 1}: \"{MergeLeftMarker}\" can not merge left from the first column.");
                        }
                        owners[(rowIndex, columnIndex)] = owners[(rowIndex, columnIndex - 1)];
                    }
                    else if (marker == MergeUpMarker)
                    {
                        if (!owners.TryGetValue((rowIndex - 1, columnIndex), out var owner))
                        {
                            throw new Exception($"Table row {rowIndex + 1}, column {columnIndex + 1}: \"{MergeUpMarker}\" has no cell above it to merge into.");
                        }
                        if (rowIndex == 1 && isFirstRowHeader)
                        {
                            throw new Exception($"Table row {rowIndex + 1}, column {columnIndex + 1}: \"{MergeUpMarker}\" can not merge into the header row.");
                        }
                        owners[(rowIndex, columnIndex)] = owner;
                    }
                    else
                    {
                        owners[(rowIndex, columnIndex)] = (rowIndex, columnIndex);
                    }
                }
            }

            var spans = new Dictionary<(int Row, int Column), (int ColSpan, int RowSpan)>();

            foreach (var group in owners.GroupBy(o => o.Value, o => o.Key))
            {
                var anchor = group.Key;
                int colSpan = group.Max(o => o.Column) - anchor.Column + 1;
                int rowSpan = group.Max(o => o.Row) - anchor.Row + 1;

                //Every merged block must be a filled rectangle, otherwise it can not be expressed with colspan/rowspan.
                if (group.Count() != colSpan * rowSpan || group.Any(o => o.Column < anchor.Column))
                {
                    throw new Exception($"Table row {anchor.Row + 1}, column {anchor.Column + 1}: merged cells must form a rectangle.");
                }

                spans[anchor] = (colSpan, rowSpan);
            }

            return spans;
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
