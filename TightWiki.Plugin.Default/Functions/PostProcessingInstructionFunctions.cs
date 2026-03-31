using System.Text;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Models;

namespace TightWiki.Plugin.Default.Functions
{
    [TwPluginModule("Post Processing Instruction Functions", "Built-in post processing instruction functions.", 1000)]
    public class PostProcessingInstructionFunctions
        : ITwPlugin
    {
        [TwPostProcessingInstructionFunction("Tags", "Displays list of tag links for the tags that are included on the current page.")]
        public async Task<TwPluginResult> Tags(ITwEngineState state,
            TightWikiTabularStyle styleName)
        {
            var html = new StringBuilder();

            switch (styleName)
            {
                case TightWikiTabularStyle.List:
                case TightWikiTabularStyle.Full:
                    html.Append("<ul>");
                    foreach (var tag in state.Tags)
                    {
                        html.Append($"<li><a href=\"{state.Engine.WikiConfiguration.BasePath}/Tags/Browse/{tag}\">{tag}</a>");
                    }
                    html.Append("</ul>");
                    break;
                case TightWikiTabularStyle.Flat:
                    foreach (var tag in state.Tags)
                    {
                        if (html.Length > 0) html.Append(" | ");
                        html.Append($"<a href=\"{state.Engine.WikiConfiguration.BasePath}/Tags/Browse/{tag}\">{tag}</a>");
                    }
                    break;
            }

            return new TwPluginResult(html.ToString());
        }

        [TwPostProcessingInstructionFunction("TagCloud", "Displays a tag cloud for the specified page tag.")]
        public async Task<TwPluginResult> TagCloud(ITwEngineState state,
            string pageTag, int top = 1000)
        {
            string html = await TwTagCloudBuilder.Build(state.Engine.DatabaseManager.PageRepository, state.Engine.WikiConfiguration.BasePath, pageTag, top);
            return new TwPluginResult(html);
        }

        [TwPostProcessingInstructionFunction("SearchCloud", "Displays a search cloud for the specified search phrase.")]
        public async Task<TwPluginResult> SearchCloud(ITwEngineState state,
            string searchPhrase, int top = 1000)
        {
            var tokens = searchPhrase.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList();

            string html = await TwSearchCloudBuilder.Build(state.Engine.DatabaseManager.PageRepository, state.Engine.WikiConfiguration.BasePath, tokens, top);
            return new TwPluginResult(html);
        }

        [TwPostProcessingInstructionFunction("Toc", "Displays a table of contents for the page based on the header tags.")]
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