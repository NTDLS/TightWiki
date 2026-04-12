using NTDLS.Helpers;
using System.Text;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Engine;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Library;
using TightWiki.Plugin.Models;

namespace TightWiki.Plugin.Default.StandardFunctions
{
    [TwPlugin("Files, Media & Assets", "Built-in standard functions.")]
    public class MediaFunctions
    {
        [TwStandardFunctionPlugin("Attachments", "Creates a list of all attachments for a page.")]
        public async Task<TwPluginResult> Attachments(ITwEngineState state,
            TwListStyle styleName = TwListStyle.Full, int pageSize = 5, bool pageSelector = true, string? pageName = null)
        {
            string refTag = state.GetNextTagMarker("Attachments");

            int pageNumber = int.Parse(state.QueryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));

            var navigation = TwNamespaceNavigation.CleanAndValidate(pageName ?? state.Page.Navigation);
            var attachments = await state.Engine.DatabaseManager.PageRepository.GetPageFilesInfoByPageNavigationAndPageRevisionPaged(navigation, pageNumber, pageSize, state.Revision);
            var html = new StringBuilder();
            html.Append($"<div id=\"{refTag}\"></div>");

            if (attachments.Count > 0)
            {
                html.Append("<ul>");
                foreach (var file in attachments)
                {
                    if (state.Revision != null)
                    {
                        html.Append($"<li><a href=\"{state.Engine.WikiConfiguration.BasePath}/Page/Binary/{state.Page.Navigation}/{file.FileNavigation}/{state.Revision}\">{file.Name}</a>");
                    }
                    else
                    {
                        html.Append($"<li><a href=\"{state.Engine.WikiConfiguration.BasePath}/Page/Binary/{state.Page.Navigation}/{file.FileNavigation}\">{file.Name} </a>");
                    }

                    if (styleName == TwListStyle.Full)
                    {
                        html.Append($" - ({file.FriendlySize})");
                    }

                    html.Append("</li>");
                }
                html.Append("</ul>");

                if (pageSelector && attachments.Count > 0 && attachments.First().PaginationPageCount > 1)
                {
                    html.Append(TwPageSelectorGenerator.Generate(state.QueryString, attachments.First().PaginationPageCount, refTag));
                }
            }

            return new TwPluginResult(html.ToString());
        }

        [TwStandardFunctionPlugin("Image", "Displays an image that is attached to the page.", isLitePermissiable: true)]
        public async Task<TwPluginResult> Image(ITwEngineState state,
            string name, int? scale = null, string? altText = null, string? @class = null, int? maxWidth = null)
        {
            altText ??= name;
            @class ??= "img-fluid";

            bool explicitNamespace = name.Contains("::");
            bool isPageForeignImage = false;

            if (@class != null)
            {
                @class = $"class=\"{@class}\"";
            }
            else
            {
                @class = "class=\"img-fluid\"";
            }

            string navigation = state.Page.Navigation;
            if (name.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
            {
                string image = $"<a href=\"{name}\" target=\"_blank\"><img src=\"{name}\" border=\"0\" alt=\"{altText}\" {@class} /></a>";
                return new TwPluginResult(image);
            }
            else if (name.Contains('/'))
            {
                //Allow loading attached images from other pages.
                int slashIndex = name.IndexOf('/');
                navigation = TwNamespaceNavigation.CleanAndValidate(name.Substring(0, slashIndex));
                name = name.Substring(slashIndex + 1);
                isPageForeignImage = true;
            }

            if (explicitNamespace == false && state.Page.Namespace != null)
            {
                if (state.Engine.DatabaseManager.PageRepository.GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(navigation, TwNamespaceNavigation.CleanAndValidate(name), state.Revision) == null)
                {
                    //If the image does not exist, and no namespace was specified, but the page has a namespace - then default to the pages namespace.
                    navigation = TwNamespaceNavigation.CleanAndValidate($"{state.Page.Namespace}::{name}");
                }
            }

            var queryParams = new List<string>();
            if (scale != 100) queryParams.Add($"Scale={scale}");
            if (maxWidth != null) queryParams.Add($"MaxWidth={maxWidth}");

            if (state.Revision != null && isPageForeignImage == false)
            {
                //Check for isPageForeignImage because we don't version foreign page files.
                string link = $"/Page/Image/{navigation}/{TwNamespaceNavigation.CleanAndValidate(name)}/{state.Revision}";
                string image = $"<a href=\"{state.Engine.WikiConfiguration.BasePath}{link}\" target=\"_blank\"><img src=\"{state.Engine.WikiConfiguration.BasePath}{link}?{string.Join('&', queryParams)}\" border=\"0\" alt=\"{altText}\" {@class} /></a>";
                return new TwPluginResult(image);
            }
            else
            {
                string link = $"/Page/Image/{navigation}/{TwNamespaceNavigation.CleanAndValidate(name)}";
                string image = $"<a href=\"{state.Engine.WikiConfiguration.BasePath}{link}\" target=\"_blank\"><img src=\"{state.Engine.WikiConfiguration.BasePath}{link}?{string.Join('&', queryParams)}\" border=\"0\" alt=\"{altText}\" {@class} /></a>";
                return new TwPluginResult(image);
            }
        }

        [TwStandardFunctionPlugin("File", "Displays a file download link.")]
        public async Task<TwPluginResult> File(ITwEngineState state, string name, string linkText, bool showSize = false)
        {
            bool explicitNamespace = name.Contains("::");
            bool isPageForeignFile = false;

            string navigation = state.Page.Navigation;
            if (name.Contains('/'))
            {
                //Allow loading attached images from other pages.
                int slashIndex = name.IndexOf("/");
                navigation = TwNamespaceNavigation.CleanAndValidate(name.Substring(0, slashIndex));
                name = name.Substring(slashIndex + 1);
                isPageForeignFile = true;
            }

            if (explicitNamespace == false && state.Page.Namespace != null)
            {
                if (state.Engine.DatabaseManager.PageRepository.GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(navigation, TwNamespaceNavigation.CleanAndValidate(name), state.Revision) == null)
                {
                    //If the image does not exist, and no namespace was specified, but the page has a namespace - then default to the pages namespace.
                    navigation = TwNamespaceNavigation.CleanAndValidate($"{state.Page.Namespace}::{name}");
                }
            }

            var attachment = await state.Engine.DatabaseManager.PageRepository.GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(navigation, TwNamespaceNavigation.CleanAndValidate(name), state.Revision);
            if (attachment != null)
            {
                string alt = linkText ?? name;

                if (showSize)
                {
                    alt += $" ({attachment.FriendlySize})";
                }

                if (state.Revision != null && isPageForeignFile == false)
                {
                    //Check for isPageForeignImage because we don't version foreign page files.
                    string link = $"/Page/Binary/{navigation}/{TwNamespaceNavigation.CleanAndValidate(name)}/{state.Revision}";
                    string image = $"<a href=\"{state.Engine.WikiConfiguration.BasePath}{link}\">{alt}</a>";
                    return new TwPluginResult(image);
                }
                else
                {
                    string link = $"/Page/Binary/{navigation}/{TwNamespaceNavigation.CleanAndValidate(name)}";
                    string image = $"<a href=\"{state.Engine.WikiConfiguration.BasePath}{link}\">{alt}</a>";
                    return new TwPluginResult(image);
                }
            }
            throw new Exception($"File not found [{name}]");
        }
    }
}
