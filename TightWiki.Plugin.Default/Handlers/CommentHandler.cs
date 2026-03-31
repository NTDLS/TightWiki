using Microsoft.Extensions.Logging;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Handlers;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;

namespace TightWiki.Plugin.Default.Handlers
{
    /// <summary>
    /// Handles wiki comments. These are generally removed from the result.
    /// </summary>
    [TwPlugin("Default handlers", "Handles various TightWiki instructions.", 1000)]
    public class Handlers
    {
        /// <summary>
        /// Handles a wiki comment.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="text">The comment text</param>
        [TwCommentPluginHandler("Default comment handler", "Handles wiki comments.", 1000)]
        public async Task<TwPluginResult> Handle(ITwEngineState state, string text)
        {
            return new TwPluginResult() { Instructions = [TwResultInstruction.TruncateTrailingLine] };
        }


        /// <summary>
        /// Handles wiki completion events. Is called when the wiki processing completes for a given page.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        [TwCompletionPluginHandler("Default completion handler", "Handles wiki completion events.", 1000)]
        public async Task<TwPluginResult> Handle(ITwEngineState state)
        {
            if (state.Engine.WikiConfiguration.RecordCompilationMetrics)
            {
                await state.Engine.DatabaseManager.StatisticsRepository.MergePageCompilationStatistics(state.Page.Id,
                    state.ProcessingTime.TotalMilliseconds,
                    state.MatchCount,
                    state.ErrorCount,
                    state.OutgoingLinks.Count,
                    state.Tags.Count,
                    state.HtmlResult.Length,
                    state.Page.Body.Length);
            }

            return new TwPluginResult();
        }

        /// <summary>
        /// Handles an emoji instruction.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="key">The lookup key for the given emoji.</param>
        /// <param name="scale">The desired 1-100 scale factor for the emoji.</param>
        [TwEmojiPluginHandler("Default emoji handler", "Handles wiki emojis.", 1000)]
        public async Task<TwPluginResult> Handle(ITwEngineState state, string key, int scale)
        {
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
        /// Called when an exception is thrown by the wiki engine.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="ex">Optional exception, in the case that this was an actual exception.</param>
        /// <param name="customText">Text that accompanies the exception.</param>
        [TwExceptionPluginHandler("Default exception handler", "Handles exceptions thrown by the wiki engine.", 1000)]
        public async Task<TwPluginResult> Handle(ITwEngineState state, LogLevel level, string text, Exception? ex = null)
        {
            if (ex != null)
            {
                state.Logger.Log(level, text, ex);
            }
            else
            {
                state.Logger.Log(level, text);
            }

            return new TwPluginResult();
        }

        /// <summary>
        /// Handles an internal wiki link.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="link">The address of the external site being linked to.</param>
        /// <param name="text">The text which should be show in the absence of an image.</param>
        /// <param name="image">The image that should be shown.</param>
        /// <param name="imageScale">The 0-100 image scale factor for the given image.</param>
        [TwExternalLinkPluginHandler("Default external link handler", "Handles links the wiki to another site.", 1000)]
        public async Task<TwPluginResult> Handle(ITwEngineState state, string link, string? text, string? image)
        {
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
        /// Handles wiki headings. These are automatically added to the table of contents.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="depth">The size of the header, also used for table of table of contents indentation.</param>
        /// <param name="link">The self link reference.</param>
        /// <param name="text">The text for the self link.</param>
        [TwHeadingPluginHandler("Default heading handler", "Handles wiki headings.", 1000)]
        public async Task<TwPluginResult> Handle(ITwEngineState state, int depth, string link, string text)
        {
            depth = Math.Clamp(depth, 1, 6);
            string html = $"""<div class="tw-heading tw-heading-{depth}" id="{link}"><a href="#{link}">{text}</a></div>""";
            return new TwPluginResult(html);
        }

        /// <summary>
        /// Handles basic markup instructions like bold, italic, underline, etc.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="sequence">The sequence of symbols that were found to denotate this markup instruction,</param>
        /// <param name="scopeBody">The body of text to apply the style to.</param>
        [TwMarkupPluginHandler("Default markup handler", "Handles basic markup instructions like bold, italic, underline, etc.", 1000)]
        public async Task<TwPluginResult> Handle(ITwEngineState state, char sequence, string scopeBody)
        {
            switch (sequence)
            {
                case '~': return new TwPluginResult($"<strike>{scopeBody}</strike>");
                case '*': return new TwPluginResult($"<strong>{scopeBody}</strong>");
                case '_': return new TwPluginResult($"<u>{scopeBody}</u>");
                case '/': return new TwPluginResult($"<i>{scopeBody}</i>");
                case '!': return new TwPluginResult($"<mark>{scopeBody}</mark>");
                default:
                    break;
            }

            return new TwPluginResult() { Instructions = [TwResultInstruction.Skip] };
        }

        /// <summary>
        /// Handles an internal wiki link.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="pageNavigation">The navigation for the linked page.</param>
        /// <param name="pageName">The name of the page being linked to.</param>
        /// <param name="linkText">The text which should be show in the absence of an image.</param>
        /// <param name="image">The image that should be shown.</param>
        /// <param name="imageScale">The 0-100 image scale factor for the given image.</param>
        [TwInternalLinkPluginHandler("Default internal link handler", "Handles links from one wiki page to another.", 1000)]
        public async Task<TwPluginResult> Handle(ITwEngineState state, TwNamespaceNavigation pageNavigation,
            string pageName, string linkText, string? image, int imageScale)
        {
            var page = await state.Engine.DatabaseManager.PageRepository.GetPageRevisionByNavigation(pageNavigation);

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
                            href = $"<a href=\"{state.Engine.WikiConfiguration.BasePath}/Page/Create?Name={pageName}\">{linkText}</a>";
                        }

                        return new TwPluginResult(href)
                        {
                            Instructions = [TwResultInstruction.DisallowNestedProcessing]
                        };
                    }
                    else if (linkText != null)
                    {
                        var href = $"<a href=\"{state.Engine.WikiConfiguration.BasePath}/Page/Create?Name={pageName}\">{linkText}</a>"
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
                    else if (linkText != null)
                    {
                        return new TwPluginResult(linkText)
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
                    href = $"<a href=\"{state.Engine.WikiConfiguration.BasePath}/{page.Navigation}\">{linkText}</a>";
                }

                return new TwPluginResult(href)
                {
                    Instructions = [TwResultInstruction.DisallowNestedProcessing]
                };
            }
        }
    }
}
