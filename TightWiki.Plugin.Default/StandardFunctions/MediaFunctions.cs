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
            var classAttribute = $"class=\"{WebUtility.HtmlEncode(@class ?? "img-fluid")}\"";

            if (name.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase)
                || name.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
            {
                var url = WebUtility.HtmlEncode(name);
                return new TwPluginResult($"<a href=\"{url}\" target=\"_blank\"><img src=\"{url}\" border=\"0\" alt=\"{WebUtility.HtmlEncode(altText)}\" {classAttribute} /></a>");
            }

            //An image that is not found still gets its tag, so the page shows where it belongs.
            var attachment = await ResolveAttachment(state, name);
            var link = $"{state.Engine.WikiConfiguration.BasePath}/Page/Image/{attachment.PageNavigation}/{attachment.FileNavigation}"
                + (attachment.PageRevision != null ? $"/{attachment.PageRevision}" : string.Empty);

            var queryParams = new List<string>();
            if (scale != null && scale != 100) queryParams.Add($"Scale={scale}");
            if (maxWidth != null) queryParams.Add($"MaxWidth={maxWidth}");
            var source = queryParams.Count > 0 ? $"{link}?{string.Join('&', queryParams)}" : link;

            return new TwPluginResult($"<a href=\"{WebUtility.HtmlEncode(link)}\" target=\"_blank\"><img src=\"{WebUtility.HtmlEncode(source)}\" border=\"0\" alt=\"{WebUtility.HtmlEncode(altText)}\" {classAttribute} /></a>");
        }

        /// <summary>
        /// Where an attachment named in a function is, and its details if it exists.
        /// </summary>
        /// <param name="PageRevision">The revision of the page to read it from, null for the current revision.</param>
        private record ResolvedAttachment(string PageNavigation, string FileNavigation, int? PageRevision, TwPageFileAttachmentInfo? Info);

        /// <summary>
        /// Finds an attachment named in a function: "file.png" on this page, or "Page Name/file.png" on another page.
        /// For another page named without a namespace, when the file is not found there it is also looked for on the page
        ///  of that name in this page's namespace, so pages in a namespace can refer to each other by their short names.
        /// </summary>
        private static async Task<ResolvedAttachment> ResolveAttachment(ITwEngineState state, string name)
        {
            var repository = state.Engine.DatabaseManager.PageRepository;

            int slashIndex = name.IndexOf('/');
            if (slashIndex < 0)
            {
                //Files on this page are versioned with it.
                var fileNavigation = TwNamespaceNavigation.CleanAndValidate(name);
                var info = await repository.GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(state.Page.Navigation, fileNavigation, state.Revision);
                return new ResolvedAttachment(state.Page.Navigation, fileNavigation, state.Revision, info);
            }

            var pageName = name.Substring(0, slashIndex);
            var foreignFileNavigation = TwNamespaceNavigation.CleanAndValidate(name.Substring(slashIndex + 1));

            //Files on other pages are not versioned with this page, so they are always that page's current revision.
            var pageNavigation = TwNamespaceNavigation.CleanAndValidate(pageName);
            var foreignInfo = await repository.GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(pageNavigation, foreignFileNavigation, null);

            if (foreignInfo == null && !pageName.Contains("::") && !string.IsNullOrWhiteSpace(state.Page.Namespace))
            {
                var namespacedNavigation = TwNamespaceNavigation.CleanAndValidate($"{state.Page.Namespace} :: {pageName}");
                var namespacedInfo = await repository.GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(namespacedNavigation, foreignFileNavigation, null);
                if (namespacedInfo != null)
                {
                    return new ResolvedAttachment(namespacedNavigation, foreignFileNavigation, null, namespacedInfo);
                }
            }

            return new ResolvedAttachment(pageNavigation, foreignFileNavigation, null, foreignInfo);
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
                throw new Exception($"File not found [{name}]");
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
                throw new Exception($"File not found [{name}]");
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
            var attachment = await ResolveAttachment(state, name);
            return (BinaryUrl(state, attachment), attachment.Info);
        }

        /// <summary>
        /// The URL that sends an attachment's content, with range support so video can be seeked.
        /// </summary>
        private static string BinaryUrl(ITwEngineState state, ResolvedAttachment attachment)
            => $"{state.Engine.WikiConfiguration.BasePath}/Page/Binary/{attachment.PageNavigation}/{attachment.FileNavigation}"
            + (attachment.PageRevision != null ? $"/{attachment.PageRevision}" : string.Empty);

        private static string DownloadLink(ITwEngineState state, string url, TwPageFileAttachmentInfo attachment)
            => $"<a href=\"{WebUtility.HtmlEncode(url)}\">{WebUtility.HtmlEncode(string.Format(state.Localizer["Download {0}"].Value, attachment.Name))}</a>";

        private static string NotPlayable(ITwEngineState state, string url, TwPageFileAttachmentInfo attachment)
            => $"<div class=\"alert alert-secondary\" role=\"note\">"
            + WebUtility.HtmlEncode(state.Localizer["This file can not be played in the browser."].Value)
            + $" {DownloadLink(state, url, attachment)} ({WebUtility.HtmlEncode(attachment.FriendlySize)})</div>";

        [TwStandardFunctionPlugin("File", "Displays a file download link.")]
        public async Task<TwPluginResult> File(ITwEngineState state, string name, string linkText, bool showSize = false)
        {
            var attachment = await ResolveAttachment(state, name);
            if (attachment.Info == null)
            {
                throw new Exception($"File not found [{name}]");
            }

            string text = linkText ?? name;
            if (showSize)
            {
                text += $" ({attachment.Info.FriendlySize})";
            }

            return new TwPluginResult($"<a href=\"{WebUtility.HtmlEncode(BinaryUrl(state, attachment))}\">{WebUtility.HtmlEncode(text)}</a>");
        }
    }
}
