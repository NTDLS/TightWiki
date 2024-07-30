using TightWiki.Engine.Library;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.Repository;
using static TightWiki.Engine.Library.Constants;

namespace TightWiki.Engine.Implementation
{
    /// <summary>
    /// Handles links from one wiki page to another.
    /// </summary>
    public class InternalLinkHandler : IInternalLinkHandler
    {
        /// <summary>
        /// Handles an internal wiki link.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="pageNavigation">The navigation for the linked page.</param>
        /// <param name="pageName">The name of the page being linked to.</param>
        /// <param name="linkText">The text which should be show in the absence of an image.</param>
        /// <param name="image">The image that should be shown.</param>
        /// <param name="imageScale">The 0-100 image scale factor for the given image.</param>
        public HandlerResult Handle(ITightEngineState state, NamespaceNavigation pageNavigation,
            string pageName, string linkText, string? image, int imageScale)
        {
            var page = PageRepository.GetPageRevisionByNavigation(pageNavigation);

            if (page == null)
            {
                if (state.Session?.CanCreate == true)
                {
                    if (image != null)
                    {
                        string href;

                        if (image.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase)
                            || image.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
                        {
                            //The image is external.
                            href = $"<a href=\"/Page/Create?Name={pageName}\"><img src=\"{image}?Scale={imageScale}\" /></a>";
                        }
                        else if (image.Contains('/'))
                        {
                            //The image is located on another page.
                            href = $"<a href=\"/Page/Create?Name={pageName}\"><img src=\"/Page/Image/{image}?Scale={imageScale}\" /></a>";
                        }
                        else
                        {
                            //The image is located on this page, but this page does not exist.
                            href = $"<a href=\"/Page/Create?Name={pageName}\">{linkText}</a>";
                        }

                        return new HandlerResult(href)
                        {
                            Instructions = [HandlerResultInstruction.DisallowNestedProcessing]
                        };
                    }
                    else if (linkText != null)
                    {
                        var href = $"<a href=\"/Page/Create?Name={pageName}\">{linkText}</a>"
                            + "<font color=\"#cc0000\" size=\"2\">?</font>";

                        return new HandlerResult(href)
                        {
                            Instructions = [HandlerResultInstruction.DisallowNestedProcessing]
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
                            mockHref = $"<img src=\"{image}?Scale={imageScale}\" />";
                        }
                        else if (image.Contains('/'))
                        {
                            //The image is located on another page.
                            mockHref = $"<img src=\"/Page/Image/{image}?Scale={imageScale}\" />";
                        }
                        else
                        {
                            //The image is located on this page, but this page does not exist.
                            mockHref = $"linkText";
                        }

                        return new HandlerResult(mockHref)
                        {
                            Instructions = [HandlerResultInstruction.DisallowNestedProcessing]
                        };
                    }
                    else if (linkText != null)
                    {
                        return new HandlerResult(linkText)
                        {
                            Instructions = [HandlerResultInstruction.DisallowNestedProcessing]
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
                        href = $"<a href=\"/{page.Navigation}\"><img src=\"{image}\" /></a>";
                    }
                    else if (image.Contains('/'))
                    {
                        //The image is located on another page.
                        href = $"<a href=\"/{page.Navigation}\"><img src=\"/Page/Image/{image}?Scale={imageScale}\" /></a>";
                    }
                    else
                    {
                        //The image is located on this page.
                        href = $"<a href=\"/{page.Navigation}\"><img src=\"/Page/Image/{state.Page.Navigation}/{image}?Scale={imageScale}\" /></a>";
                    }
                }
                else
                {
                    //Just a plain ol' internal page link.
                    href = $"<a href=\"/{page.Navigation}\">{linkText}</a>";
                }

                return new HandlerResult(href)
                {
                    Instructions = [HandlerResultInstruction.DisallowNestedProcessing]
                };
            }
        }
    }
}
