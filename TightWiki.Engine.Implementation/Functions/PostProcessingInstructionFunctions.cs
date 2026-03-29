using System.Text;
using TightWiki.Engine.Implementation.Utility;
using TightWiki.Engine.Library;
using TightWiki.Engine.Library.Function.Attributes;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.Library;

namespace TightWiki.Engine.Implementation.Functions
{
    [TightWikiFunctionModule("Post Processing Instruction Functions", "Built-in post processing instruction functions.")]
    public class PostProcessingInstructionFunctions
        : ITightWikiFunctionModule
    {
        [TightWikiPostProcessingInstructionFunction("Tags", "Displays list of tag links for the tags that are included on the current page.")]
        public async Task<HandlerResult> Tags(ITightEngineState state,
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

            return new HandlerResult(html.ToString());
        }

        [TightWikiPostProcessingInstructionFunction("TagCloud", "Displays a tag cloud for the specified page tag.")]
        public async Task<HandlerResult> TagCloud(ITightEngineState state,
            string pageTag, int top = 1000)
        {
            string html = await TagCloudBuilder.Build(state.Engine.WikiConfiguration.BasePath, pageTag, top);
            return new HandlerResult(html);
        }

        [TightWikiPostProcessingInstructionFunction("SearchCloud", "Displays a search cloud for the specified search phrase.")]
        public async Task<HandlerResult> SearchCloud(ITightEngineState state,
            string searchPhrase, int top = 1000)
        {
            var tokens = searchPhrase.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList();

            string html = await SearchCloudBuilder.Build(state.Engine.WikiConfiguration.BasePath, tokens, top);
            return new HandlerResult(html);
        }

        [TightWikiPostProcessingInstructionFunction("Toc", "Displays a table of contents for the page based on the header tags.")]
        public async Task<HandlerResult> Toc(ITightEngineState state,
            bool alphabetized = false)
        {
            var html = new StringBuilder();

            var tags = (from t in state.TableOfContents
                        orderby t.StartingPosition
                        select t).ToList();

            var unordered = new List<TableOfContentsTag>();
            var ordered = new List<TableOfContentsTag>();

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

            return new HandlerResult(html.ToString());
        }
    }
}