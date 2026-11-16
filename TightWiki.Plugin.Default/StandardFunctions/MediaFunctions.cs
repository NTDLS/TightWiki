using NTDLS.Helpers;
using System.Net;
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
                    html.Append(TwPageSelectorGenerator.Generate(state.QueryString, attachments.First().PaginationPageCount, refTag, localizer: state.Localizer));
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

        /// <summary>
        /// Video formats that browsers can play. Others (.avi, .mov, .wmv, .mkv) are offered as a download.
        /// </summary>
        private static readonly HashSet<string> PlayableVideoTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "video/mp4", "video/webm", "video/ogg"
        };

        /// <summary>
        /// Audio formats that browsers can play.
        /// </summary>
        private static readonly HashSet<string> PlayableAudioTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "audio/mpeg", "audio/mp3", "audio/mp4", "audio/x-m4a", "audio/aac", "audio/ogg", "application/ogg",
            "audio/wav", "audio/x-wav", "audio/wave", "audio/vnd.wave", "audio/webm", "audio/flac", "audio/x-flac"
        };

        [TwStandardFunctionPlugin("Video", "Plays a video that is attached to the page (MP4, WebM or Ogg). Other formats are shown as a download link.")]
        public async Task<TwPluginResult> Video(ITwEngineState state,
            string name, int? width = null, string? poster = null, bool loop = false, bool muted = false, bool autoplay = false)
        {
            var media = await ResolveMedia(state, name);
            if (media.Attachment == null)
            {
                throw new Exception($"File not found [{WebUtility.HtmlEncode(name)}]");
            }

            if (!PlayableVideoTypes.Contains(media.Attachment.ContentType))
            {
                return new TwPluginResult(NotPlayable(state, media.Url, media.Attachment));
            }

            var attributes = new StringBuilder("controls preload=\"metadata\" playsinline class=\"img-fluid\"");
            if (width > 0) attributes.Append($" width=\"{width}\"");
            if (loop) attributes.Append(" loop");
            //Browsers only start playing by themselves when the sound is off.
            if (autoplay) attributes.Append(" autoplay muted");
            else if (muted) attributes.Append(" muted");

            if (!string.IsNullOrWhiteSpace(poster))
            {
                var posterMedia = await ResolveMedia(state, poster);
                if (posterMedia.Attachment != null)
                {
                    attributes.Append($" poster=\"{WebUtility.HtmlEncode(posterMedia.Url)}\"");
                }
            }

            return new TwPluginResult(
                $"<video {attributes}>"
                + $"<source src=\"{WebUtility.HtmlEncode(media.Url)}\" type=\"{WebUtility.HtmlEncode(media.Attachment.ContentType)}\" />"
                + DownloadLink(state, media.Url, media.Attachment)
                + "</video>");
        }

        [TwStandardFunctionPlugin("Audio", "Plays audio that is attached to the page (MP3, M4A, Ogg, WAV, WebM or FLAC). Other formats are shown as a download link.")]
        public async Task<TwPluginResult> Audio(ITwEngineState state, string name, bool loop = false)
        {
            var media = await ResolveMedia(state, name);
            if (media.Attachment == null)
            {
                throw new Exception($"File not found [{WebUtility.HtmlEncode(name)}]");
            }

            if (!PlayableAudioTypes.Contains(media.Attachment.ContentType))
            {
                return new TwPluginResult(NotPlayable(state, media.Url, media.Attachment));
            }

            return new TwPluginResult(
                $"<audio controls preload=\"metadata\"{(loop ? " loop" : string.Empty)}>"
                + $"<source src=\"{WebUtility.HtmlEncode(media.Url)}\" type=\"{WebUtility.HtmlEncode(media.Attachment.ContentType)}\" />"
                + DownloadLink(state, media.Url, media.Attachment)
                + "</audio>");
        }

        /// <summary>
        /// Finds an attachment by name, either on this page or, written as "Page Name/file.mp4", on another page.
        /// Returns the URL that streams it (with seeking) and its details, which are null if it does not exist.
        /// </summary>
        private static async Task<(string Url, TwPageFileAttachmentInfo? Attachment)> ResolveMedia(ITwEngineState state, string name)
        {
            string navigation = state.Page.Navigation;
            bool isPageForeign = false;

            if (name.Contains('/'))
            {
                int slashIndex = name.IndexOf('/');
                navigation = TwNamespaceNavigation.CleanAndValidate(name.Substring(0, slashIndex));
                name = name.Substring(slashIndex + 1);
                isPageForeign = true;
            }

            var fileNavigation = TwNamespaceNavigation.CleanAndValidate(name);

            //Files on other pages are not versioned with this page, so they are always the current revision.
            int? pageRevision = isPageForeign ? null : state.Revision;

            var attachment = await state.Engine.DatabaseManager.PageRepository
                .GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(navigation, fileNavigation, pageRevision);

            var url = $"{state.Engine.WikiConfiguration.BasePath}/Page/Binary/{navigation}/{fileNavigation}"
                + (pageRevision != null ? $"/{pageRevision}" : string.Empty);

            return (url, attachment);
        }

        private static string DownloadLink(ITwEngineState state, string url, TwPageFileAttachmentInfo attachment)
            => $"<a href=\"{WebUtility.HtmlEncode(url)}\">{WebUtility.HtmlEncode(string.Format(state.Localizer["Download {0}"].Value, attachment.Name))}</a>";

        private static string NotPlayable(ITwEngineState state, string url, TwPageFileAttachmentInfo attachment)
            => $"<div class=\"alert alert-secondary\" role=\"note\">"
            + WebUtility.HtmlEncode(state.Localizer["This file can not be played in the browser."].Value)
            + $" {DownloadLink(state, url, attachment)} ({WebUtility.HtmlEncode(attachment.FriendlySize)})</div>";

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
            throw new Exception($"File not found [{WebUtility.HtmlEncode(name)}]");
        }
    }
}
