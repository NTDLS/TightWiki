using Microsoft.AspNetCore.Html;
using NTDLS.Helpers;
using System.Net;

namespace TightWiki
{
    public static class TwConfirmActionHelper
    {
        /// <summary>
        /// Generates a link that navigates via GET to a "confirm action" page where the yes link is RED, but the NO button is still GREEN.
        /// </summary>
        /// <param name="message">The message to be displayed.</param>
        /// <param name="linkLabel">the label for the link that will redirect to this confirm action page.</param>
        /// <param name="controllerURL">The URL which will handle the click of the "yes" or "no" for the confirm action page.</param>
        /// <param name="yesOrDefaultRedirectURL">The URL to redirect to AFTER the controller has been called if the user selected YES (or NO, if the NO link is not specified.</param>
        /// <param name="noRedirectURL">The URL to redirect to AFTER the controller has been called if the user selected NO, if not specified, the same link that is provided to yesOrDefaultRedirectURL is used.</param>
        public static IHtmlContent GenerateDangerButton(string basePath, string message, string linkLabel, string controllerURL,
            string? yesOrDefaultRedirectURL, string? noRedirectURL = null, string? @class = "")
            => Generate("Danger", "btn-danger", basePath, message, linkLabel, controllerURL, yesOrDefaultRedirectURL, noRedirectURL, @class);

        /// <summary>
        /// Generates a link that navigates via GET to a "confirm action" page where the yes link is GREEN.
        /// </summary>
        /// <param name="message">The message to be displayed.</param>
        /// <param name="linkLabel">the label for the link that will redirect to this confirm action page.</param>
        /// <param name="controllerURL">The URL which will handle the click of the "yes" or "no" for the confirm action page.</param>
        /// <param name="yesOrDefaultRedirectURL">The URL to redirect to AFTER the controller has been called if the user selected YES (or NO, if the NO link is not specified.</param>
        /// <param name="noRedirectURL">The URL to redirect to AFTER the controller has been called if the user selected NO, if not specified, the same link that is provided to yesOrDefaultRedirectURL is used.</param>
        public static IHtmlContent GenerateSafeButton(string basePath, string message, string linkLabel, string controllerURL,
            string? yesOrDefaultRedirectURL, string? noRedirectURL = null, string? @class = "")
            => Generate("Safe", "btn-success", basePath, message, linkLabel, controllerURL, yesOrDefaultRedirectURL, noRedirectURL, @class);

        /// <summary>
        /// Generates a link that navigates via GET to a "confirm action" page where the yes link is YELLOW, but the NO button is still GREEN.
        /// </summary>
        /// <param name="message">The message to be displayed.</param>
        /// <param name="linkLabel">the label for the link that will redirect to this confirm action page.</param>
        /// <param name="controllerURL">The URL which will handle the click of the "yes" or "no" for the confirm action page.</param>
        /// <param name="yesOrDefaultRedirectURL">The URL to redirect to AFTER the controller has been called if the user selected YES (or NO, if the NO link is not specified.</param>
        /// <param name="noRedirectURL">The URL to redirect to AFTER the controller has been called if the user selected NO, if not specified, the same link that is provided to yesOrDefaultRedirectURL is used.</param>
        public static IHtmlContent GenerateWarnButton(string basePath, string message, string linkLabel, string controllerURL,
            string? yesOrDefaultRedirectURL, string? noRedirectURL = null, string? @class = "")
            => Generate("Warn", "btn-warning", basePath, message, linkLabel, controllerURL, yesOrDefaultRedirectURL, noRedirectURL, @class);

        private static readonly TimeSpan Lifetime = TimeSpan.FromHours(12);

        /// <summary>
        /// The confirm-action page renders only what is in its signed token, so a crafted link can not change its
        /// message or the action that the "yes" button posts to.
        /// </summary>
        private static IHtmlContent Generate(string style, string defaultClass, string basePath, string message, string linkLabel,
            string controllerURL, string? yesOrDefaultRedirectURL, string? noRedirectURL, string? @class)
        {
            noRedirectURL ??= yesOrDefaultRedirectURL;

            var payload = new TwConfirmActionPayload(
                ControllerURL: $"{basePath}{controllerURL}",
                YesRedirectURL: yesOrDefaultRedirectURL.EnsureNotNull(),
                NoRedirectURL: noRedirectURL.EnsureNotNull(),
                Message: message,
                Style: style);

            if (string.IsNullOrEmpty(@class))
            {
                @class = defaultClass;
            }

            return new HtmlString($"<a class=\"btn {@class}\" href=\"{basePath}/Utility/ConfirmAction?t={TwSignedUrl.Protect(payload, Lifetime)}\">{WebUtility.HtmlEncode(linkLabel)}</a>");
        }
    }
}
