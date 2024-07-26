using System.Text;
using TightWiki.Engine.Implementation;
using TightWiki.Engine.Library;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.EngineFunction;
using static TightWiki.Engine.Library.Constants;

namespace TightWiki.Engine.Handlers
{
    public class StandardPostProcessingFunctionHandler : IFunctionHandler
    {
        private readonly Dictionary<string, int> _sequences = new();

        public FunctionPrototypeCollection Prototypes()
        {
            return StandardPostProcessingFunctionPrototypes.Collection;
        }

        public HandlerResult Handle(IWikifier wikifier, FunctionCall function, string scopeBody)
        {
            switch (function.Name.ToLower())
            {
                //------------------------------------------------------------------------------------------------------------------------------
                //Displays a tag link list.
                case "tags": //##tags
                    {
                        string styleName = function.Parameters.Get<string>("styleName").ToLower();
                        var html = new StringBuilder();

                        if (styleName == "list")
                        {
                            html.Append("<ul>");
                            foreach (var tag in wikifier.Tags)
                            {
                                html.Append($"<li><a href=\"/Tag/Browse/{tag}\">{tag}</a>");
                            }
                            html.Append("</ul>");
                        }
                        else if (styleName == "flat")
                        {
                            foreach (var tag in wikifier.Tags)
                            {
                                if (html.Length > 0) html.Append(" | ");
                                html.Append($"<a href=\"/Tag/Browse/{tag}\">{tag}</a>");
                            }
                        }

                        return new HandlerResult(html.ToString());
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "tagcloud":
                    {
                        var top = function.Parameters.Get<int>("Top");
                        string seedTag = function.Parameters.Get<string>("pageTag");

                        string html = TagCloud.Build(seedTag, top);
                        return new HandlerResult(html);
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "searchcloud":
                    {
                        var top = function.Parameters.Get<int>("Top");
                        var tokens = function.Parameters.Get<string>("searchPhrase").Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList();

                        string html = SearchCloud.Build(tokens, top);
                        return new HandlerResult(html);
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                //Diplays a table of contents for the page based on the header tags.
                case "toc":
                    {
                        bool alphabetized = function.Parameters.Get<bool>("alphabetized");

                        var html = new StringBuilder();

                        var tags = (from t in wikifier.TableOfContents
                                    orderby t.StartingPosition
                                    select t).ToList();

                        var unordered = new List<TOCTag>();
                        var ordered = new List<TOCTag>();

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

            return new HandlerResult() { Instructions = [HandlerResultInstruction.Skip] };
        }
    }
}