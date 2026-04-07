using System.Web;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Handlers;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Function;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Models;

namespace TightWiki.Plugin.Default
{
    /// <summary>
    /// Handler functions for various wiki operations.
    /// </summary>
    [TwPlugin("Markup Handlers", "Handles various wiki markup tags.", 1)]
    public class MarkupHandlers
    {
        //The order of transformations is important and is set by the precedence parameter
        //  of the attribute, the first thing we need to do is transform literals so that
        //  the content inside them is not transformed by any other handlers and is displayed
        //  verbatim on the page. For example, if we have a code block with some wiki markup
        //  inside it, we don't want that wiki markup to be transformed, we want it to be
        //  displayed as-is.

        /// <summary>
        /// Handles a literal string. This is used to prevent wiki processing on a given string.
        /// For example, {{[[This will not be processed as a link]]}}.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="match">The matched string from the regular expression</param>
        [TwMarkupPluginHandler("Default literals handler", "Handles literal strings.", precedence: 10)]
        [TwPluginRegularExpression("{{([\\S\\s]*)}}")]
        public async Task<TwPluginResult> HandleLiterals(ITwEngineState state, string match)
        {
            string value = match.Substring(2, match.Length - 4);
            value = HttpUtility.HtmlEncode(value);

            return new TwPluginResult(value)
            {
                Instructions = [TwResultInstruction.DisallowNestedProcessing]
            };
        }

        /// <summary>
        /// Handles a wiki comment.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="match">The matched string from the regular expression</param>
        [TwMarkupPluginHandler("Default comment handler", "Handles wiki comments.", precedence: 20)]
        [TwPluginRegularExpression("(\\%\\%.+?\\%\\%)")]
        public async Task<TwPluginResult> HandleComment(ITwEngineState state, string text)
        {
            return new TwPluginResult() { Instructions = [TwResultInstruction.TruncateTrailingLine] };
        }

        /// <summary>
        /// Transform headings. These are the basic HTML H1-H6 headings but they are saved for the building of the table of contents.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="match">The matched string from the regular expression</param>
        [TwMarkupPluginHandler("Default heading handler", "Handles wiki headings.", precedence: 30)]
        [TwPluginRegularExpression("^(={2,7}.*)", multiline: true)]
        public async Task<TwPluginResult> HandleWikiHeadings(ITwEngineState state, string match)
        {

            int headingMarkers = 0;
            foreach (char c in match)
            {
                if (c != '=')
                {
                    break;
                }
                headingMarkers++;
            }
            if (headingMarkers >= 2)
            {
                string link = state.TocName + "_" + state.TableOfContents.Count.ToString();
                string text = match.Substring(headingMarkers).Trim().Trim(['=']).Trim();

                int depth = headingMarkers - 1;

                depth = Math.Clamp(depth, 1, 6);
                state.TableOfContents.Add(new TwTableOfContentsTag(headingMarkers - 1, state.TableOfContents.Count, link, text));
                string html = $"""<div class="tw-heading tw-heading-{depth}" id="{link}"><a href="#{link}">{text}</a></div>""";
                return new TwPluginResult(html);
            }

            return new TwPluginResult()
            {
                Instructions = [TwResultInstruction.Skip]
            };
        }

        /// <summary>
        /// Handles variable markup instructions like ${variable}.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="match">The matched string from the regular expression</param>
        [TwMarkupPluginHandler("Default variable handler", "Handles variable markup.", precedence: 40)]
        [TwPluginRegularExpression(@"(\$\{.+?\})")]
        public async Task<TwPluginResult> HandleVariable(ITwEngineState state, string match)
        {
            string key = match.Trim(['{', '}', ' ', '\t', '$']);
            if (key.Contains('='))
            {
                var sections = key.Split('=');
                key = sections[0].Trim();
                var value = sections[1].Trim();

                if (!state.Variables.TryAdd(key, value))
                {
                    state.Variables[key] = value;
                }
                return new TwPluginResult() { Instructions = [TwResultInstruction.TruncateTrailingLine] };
            }
            else
            {
                if (state.Variables.TryGetValue(key, out string? value))
                {
                    return new TwPluginResult(value) { Instructions = [TwResultInstruction.TruncateTrailingLine] };
                }
                else
                {
                    throw new Exception($"The wiki variable {key} is not defined. It should be set with ##Set() before calling Get().");
                }
            }
        }

        /// <summary>
        /// Handles an internal wiki link.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="match">The matched string from the regular expression</param>
        [TwMarkupPluginHandler("Default external link handler", "Handles links the wiki to another site.", precedence: 50)]
        [TwPluginRegularExpression("(\\[\\[http\\:\\/\\/.+?\\]\\])")]
        [TwPluginRegularExpression("(\\[\\[https\\:\\/\\/.+?\\]\\])")]
        public async Task<TwPluginResult> HandleExternalLinks(ITwEngineState state, string match)
        {

            string link = match.Substring(2, match.Length - 4).Trim();
            var args = ParsedFunction.ParseArgumentsAddParenthesis(link);

            string? text = null;
            string? image = null;

            if (args.Count > 1)
            {
                text = args[1];
                link = args[0];
                string imageTag = "image:";

                if (text.StartsWith(imageTag, StringComparison.InvariantCultureIgnoreCase))
                {
                    image = text.Substring(imageTag.Length).Trim();
                    text = null;
                }
            }
            else
            {
                text = link;
            }

            if (string.IsNullOrEmpty(image))
            {
                return new TwPluginResult($"<a href=\"{link}\">{text}</a>")
                {
                    Instructions = [TwResultInstruction.DisallowNestedProcessing]
                };
            }
            else
            {
                return new TwPluginResult($"<a href=\"{link}\"><img src=\"{image}\" border =\"0\"></a>")
                {
                    Instructions = [TwResultInstruction.DisallowNestedProcessing]
                };
            }
        }

        /// <summary>
        /// Handles an internal wiki link.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="match">The matched string from the regular expression</param>
        [TwMarkupPluginHandler("Default internal link handler", "Handles links from one wiki page to another.", precedence: 60)]
        [TwPluginRegularExpression("(\\[\\[.+?\\]\\])")]
        public async Task<TwPluginResult> HandleInternalLinks(ITwEngineState state, string match)
        {
            static string GetPageNamePart(string navigation)
            {
                var parts = navigation.Trim(':').Trim().Split("::");
                if (parts.Length > 1)
                {
                    return string.Join('_', parts.Skip(1));
                }
                return navigation.Trim(':');
            }

            string keyword = match.Substring(2, match.Length - 4);
            var args = ParsedFunction.ParseArgumentsAddParenthesis(keyword);
            string pageName;
            string text;
            string? image = null;
            int imageScale = 100;

            if (args.Count == 1)
            {
                //Page navigation only.
                text = GetPageNamePart(args[0]); //Text will be page name since we have an image.
                pageName = args[0];
            }
            else if (args.Count >= 2)
            {
                //Page navigation and explicit text (possibly image).
                pageName = args[0];

                string imageTag = "image:";
                if (args[1].StartsWith(imageTag, StringComparison.InvariantCultureIgnoreCase))
                {
                    image = args[1].Substring(imageTag.Length).Trim();
                    text = GetPageNamePart(args[0]); //Text will be page name since we have an image.
                }
                else
                {
                    text = args[1]; //Explicit text.
                }

                if (args.Count >= 3)
                {
                    //Get the specified image scale.
                    if (int.TryParse(args[2], out imageScale) == false)
                    {
                        imageScale = 100;
                    }
                }
            }
            else
            {
                throw new Exception($"Invalid internal link syntax: \"{match}\".");
            }

            pageName = pageName.Trim(':');
            var pageNavigation = new TwNamespaceNavigation(pageName);
            var page = await state.Engine.DatabaseManager.PageRepository.GetPageRevisionByNavigation(pageNavigation);

            if (pageName.Trim().StartsWith("::"))
            {
                //The user explicitly specified the root (unnamed) namespace. 
            }
            else if (string.IsNullOrEmpty(pageNavigation.Namespace))
            {
                //No namespace was specified, use the current page namespace.
                pageNavigation.Namespace = page?.Namespace ?? string.Empty;
            }
            else
            {
                //Use the namespace that the user explicitly specified.
            }


            if (page == null)
            {
                if (state.Session != null && await state.Session.HoldsPermission(pageNavigation.Canonical, TwPermission.Create))
                {
                    if (image != null)
                    {
                        string href;

                        if (image.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase)
                            || image.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
                        {
                            //The image is external.
                            href = $"<a href=\"{state.Engine.WikiConfiguration.BasePath}/Page/Create?Name={pageName}\"><img src=\"{state.Engine.WikiConfiguration.BasePath}{image}?Scale={imageScale}\" /></a>";
                        }
                        else if (image.Contains('/'))
                        {
                            //The image is located on another page.
                            href = $"<a href=\"{state.Engine.WikiConfiguration.BasePath}/Page/Create?Name={pageName}\"><img src=\"{state.Engine.WikiConfiguration.BasePath}/Page/Image/{image}?Scale={imageScale}\" /></a>";
                        }
                        else
                        {
                            //The image is located on this page, but this page does not exist.
                            href = $"<a href=\"{state.Engine.WikiConfiguration.BasePath}/Page/Create?Name={pageName}\">{text}</a>";
                        }

                        return new TwPluginResult(href)
                        {
                            Instructions = [TwResultInstruction.DisallowNestedProcessing]
                        };
                    }
                    else if (text != null)
                    {
                        var href = $"<a href=\"{state.Engine.WikiConfiguration.BasePath}/Page/Create?Name={pageName}\">{text}</a>"
                            + "<font color=\"#cc0000\" size=\"2\">?</font>";

                        return new TwPluginResult(href)
                        {
                            Instructions = [TwResultInstruction.DisallowNestedProcessing]
                        };
                    }
                    else
                    {
                        throw new Exception("No link or image was specified.");
                    }
                }
                else
                {
                    //The page does not exist and the user does not have permission to create it.

                    if (image != null)
                    {
                        string mockHref;

                        if (image.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase)
                            || image.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
                        {
                            //The image is external.
                            mockHref = $"<img src=\"{state.Engine.WikiConfiguration.BasePath}{image}?Scale={imageScale}\" />";
                        }
                        else if (image.Contains('/'))
                        {
                            //The image is located on another page.
                            mockHref = $"<img src=\"{state.Engine.WikiConfiguration.BasePath}/Page/Image/{image}?Scale={imageScale}\" />";
                        }
                        else
                        {
                            //The image is located on this page, but this page does not exist.
                            mockHref = $"linkText";
                        }

                        return new TwPluginResult(mockHref)
                        {
                            Instructions = [TwResultInstruction.DisallowNestedProcessing]
                        };
                    }
                    else if (text != null)
                    {
                        return new TwPluginResult(text)
                        {
                            Instructions = [TwResultInstruction.DisallowNestedProcessing]
                        };
                    }
                    else
                    {
                        throw new Exception("No link or image was specified.");
                    }
                }
            }
            else
            {
                string href;

                if (image != null)
                {
                    if (image.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase)
                        || image.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
                    {
                        //The image is external.
                        href = $"<a href=\"{state.Engine.WikiConfiguration.BasePath}/{page.Navigation}\"><img src=\"{state.Engine.WikiConfiguration.BasePath}{image}\" /></a>";
                    }
                    else if (image.Contains('/'))
                    {
                        //The image is located on another page.
                        href = $"<a href=\"{state.Engine.WikiConfiguration.BasePath}/{page.Navigation}\"><img src=\"{state.Engine.WikiConfiguration.BasePath}/Page/Image/{image}?Scale={imageScale}\" /></a>";
                    }
                    else
                    {
                        //The image is located on this page.
                        href = $"<a href=\"{state.Engine.WikiConfiguration.BasePath}/{page.Navigation}\"><img src=\"{state.Engine.WikiConfiguration.BasePath}/Page/Image/{state.Page.Navigation}/{image}?Scale={imageScale}\" /></a>";
                    }
                }
                else
                {
                    //Just a plain ol' internal page link.
                    href = $"<a href=\"{state.Engine.WikiConfiguration.BasePath}/{page.Navigation}\">{text}</a>";
                }

                return new TwPluginResult(href)
                {
                    Instructions = [TwResultInstruction.DisallowNestedProcessing]
                };
            }
        }


        /// <summary>
        /// Handles basic markup instructions like bold, italic, underline, etc.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="match">The matched string from the regular expression</param>
        [TwMarkupPluginHandler("Default markup handler", "Handles basic markup instructions like bold, italic, underline, etc.", precedence: 70)]
        [TwPluginRegularExpression(@"\~\~(.*?)\~\~")]
        [TwPluginRegularExpression(@"\*\*(.*?)\*\*")]
        [TwPluginRegularExpression(@"__(.*?)__")]
        [TwPluginRegularExpression(@"\/\/(.*?)\/\/")]
        [TwPluginRegularExpression(@"\!\!(.*?)\!\!")]
        public async Task<TwPluginResult> HandleMarkup(ITwEngineState state, string match)
        {
            char sequence = match[0];
            string body = match.Substring(2, match.Length - 4);

            switch (sequence)
            {
                case '~': return new TwPluginResult($"<strike>{body}</strike>");
                case '*': return new TwPluginResult($"<strong>{body}</strong>");
                case '_': return new TwPluginResult($"<u>{body}</u>");
                case '/': return new TwPluginResult($"<i>{body}</i>");
                case '!': return new TwPluginResult($"<mark>{body}</mark>");
                default:
                    break;
            }

            return new TwPluginResult() { Instructions = [TwResultInstruction.Skip] };
        }

        /// <summary>
        /// Handles an emoji instruction.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="match">The matched string from the regular expression</param>
        [TwMarkupPluginHandler("Default emoji handler", "Handles wiki emojis.", precedence: 80)]
        [TwPluginRegularExpression("(\\$\\{.+?\\})")]
        public async Task<TwPluginResult> HandleEmojis(ITwEngineState state, string match)
        {
            string key = match.Trim().ToLowerInvariant().Trim('%');
            int scale = 100;

            var parts = key.Split(',');
            if (parts.Length > 1)
            {
                key = parts[0]; //Image key;
                scale = int.Parse(parts[1]); //Image scale.
            }

            key = $"%%{key}%%";

            var emoji = state.Engine.WikiConfiguration.Emojis.FirstOrDefault(o => o.Shortcut == key);

            if (state.Engine.WikiConfiguration.Emojis.Exists(o => o.Shortcut == key))
            {
                if (scale != 100 && scale > 0 && scale <= 500)
                {
                    var emojiImage = $"<img src=\"{state.Engine.WikiConfiguration.BasePath}/file/Emoji/{key.Trim('%')}?Scale={scale}\" alt=\"{emoji?.Name}\" />";

                    return new TwPluginResult(emojiImage);
                }
                else
                {
                    var emojiImage = $"<img src=\"{state.Engine.WikiConfiguration.BasePath}/file/Emoji/{key.Trim('%')}\" alt=\"{emoji?.Name}\" />";

                    return new TwPluginResult(emojiImage);
                }
            }
            else
            {
                return new TwPluginResult(key) { Instructions = [TwResultInstruction.DisallowNestedProcessing] };
            }
        }

        /// <summary>
        /// Handles basic markup instructions like bold, italic, underline, etc.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="match">The matched string from the regular expression</param>
        [TwMarkupPluginHandler("Default upsize handler", "Handles upsize markup.", precedence: 90)]
        [TwPluginRegularExpression(@"\^{2,}.*", multiline: true)]
        public async Task<TwPluginResult> HandleUpsize(ITwEngineState state, string match)
        {
            int headingMarkers = 0;
            foreach (char c in match)
            {
                if (c != '^')
                {
                    break;
                }
                headingMarkers++;
            }
            if (headingMarkers >= 2)
            {
                string value = match.Substring(headingMarkers).Trim();
                double fontSize = 2.2 - (7 - headingMarkers) * 0.2;
                string markup = $"<span class=\"mb-0\" style=\"font-size: {fontSize}rem;\">{value}</span>\r\n";
                return new TwPluginResult(markup);
            }

            return new TwPluginResult() { Instructions = [TwResultInstruction.Skip] };
        }
    }
}
