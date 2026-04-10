using System.Text;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Models;

namespace TightWiki.Plugin.Default.StandardFunctions
{
    [TwPlugin("Default Post-Processing Instructions", "Built-in post processing instruction functions.")]
    public class PostProcessingFunctions
    {
        [TwStandardFunctionPlugin("TOC", "Displays a table of contents for the page based on the header tags.", isPostProcess: true)]
        public async Task<TwPluginResult> Toc(ITwEngineState state,
            bool alphabetized = false)
        {
            var html = new StringBuilder();

            var tags = (from t in state.TableOfContents
                        orderby t.StartingPosition
                        select t).ToList();

            var unordered = new List<TwTableOfContentsTag>();
            var ordered = new List<TwTableOfContentsTag>();

            if (alphabetized)
            {
                int level = tags.FirstOrDefault()?.Level ?? 0;

                foreach (var tag in tags)
                {
                    if (level != tag.Level)
                    {
                        ordered.AddRange(unordered.OrderBy(o => o.Text));
                        unordered.Clear();
                        level = tag.Level;
                    }

                    unordered.Add(tag);
                }

                ordered.AddRange(unordered.OrderBy(o => o.Text));
                unordered.Clear();

                tags = ordered.ToList();
            }

            int currentLevel = 0;

            foreach (var tag in tags)
            {
                if (tag.Level > currentLevel)
                {
                    while (currentLevel < tag.Level)
                    {
                        html.Append("<ul>");
                        currentLevel++;
                    }
                }
                else if (tag.Level < currentLevel)
                {
                    while (currentLevel > tag.Level)
                    {

                        html.Append("</ul>");
                        currentLevel--;
                    }
                }

                html.Append("<li><a href=\"#" + tag.HrefTag + "\">" + tag.Text + "</a></li>");
            }

            while (currentLevel > 0)
            {
                html.Append("</ul>");
                currentLevel--;
            }

            return new TwPluginResult(html.ToString());
        }
    }
}