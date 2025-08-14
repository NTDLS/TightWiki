using NTDLS.Helpers;
using System.Text;

namespace TightWiki.Library
{
    public static class ConfirmActionHelper
    {
        /// <summary>
        /// Generates a link that navigates via GET to a "confirm action" page where the yes link is RED, but the NO button is still GREEN.
        /// </summary>
        /// <param name="message">The message to be displayed.</param>
        /// <param name="linkLabel">the label for the link that will redirect to this confirm action page.</param>
        /// <param name="controllerURL">The URL which will handle the click of the "yes" or "no" for the confirm action page.</param>
        /// <param name="parameter">An optional parameter to pass to the page and controller function.</param>
        /// <param name="yesOrDefaultRedirectURL">The URL to redirect to AFTER the controller has been called if the user selected YES (or NO, if the NO link is not specified.</param>
        /// <param name="noRedirectURL">The URL to redirect to AFTER the controller has been called if the user selected NO, if not specified, the same link that is provided to yesOrDefaultRedirectURL is used.</param>
        public static string GenerateDangerLink(string basePath, string message, string linkLabel, string controllerURL,
            string? yesOrDefaultRedirectURL, string? noRedirectURL = null, string? @class = "")
        {
            noRedirectURL ??= yesOrDefaultRedirectURL;

            yesOrDefaultRedirectURL.EnsureNotNull();
            noRedirectURL.EnsureNotNull();

            var param = new StringBuilder();
            param.Append($"ControllerURL={Uri.EscapeDataString($"{basePath}{controllerURL}")}");
            param.Append($"&YesRedirectURL={Uri.EscapeDataString(yesOrDefaultRedirectURL)}");
            param.Append($"&NoRedirectURL={Uri.EscapeDataString(noRedirectURL)}");
            param.Append($"&Message={Uri.EscapeDataString(message)}");
            param.Append($"&Style=Danger");

            return $"<a class=\"btn btn-danger {@class}\" href=\"{basePath}/Utility/ConfirmAction?{param}\">{linkLabel}</a>";
        }

        /// <summary>
        /// Generates a link that navigates via GET to a "confirm action" page where the yes link is GREEN.
        /// </summary>
        /// <param name="message">The message to be displayed.</param>
        /// <param name="linkLabel">the label for the link that will redirect to this confirm action page.</param>
        /// <param name="controllerURL">The URL which will handle the click of the "yes" or "no" for the confirm action page.</param>
        /// <param name="parameter">An optional parameter to pass to the page and controller function.</param>
        /// <param name="yesOrDefaultRedirectURL">The URL to redirect to AFTER the controller has been called if the user selected YES (or NO, if the NO link is not specified.</param>
        /// <param name="noRedirectURL">The URL to redirect to AFTER the controller has been called if the user selected NO, if not specified, the same link that is provided to yesOrDefaultRedirectURL is used.</param>
        public static string GenerateSafeLink(string basePath, string message, string linkLabel, string controllerURL,
            string? yesOrDefaultRedirectURL, string? noRedirectURL = null, string? @class = "")
        {
            noRedirectURL ??= yesOrDefaultRedirectURL;

            yesOrDefaultRedirectURL.EnsureNotNull();
            noRedirectURL.EnsureNotNull();

            var param = new StringBuilder();
            param.Append($"ControllerURL={Uri.EscapeDataString($"{basePath}{controllerURL}")}");
            param.Append($"&YesRedirectURL={Uri.EscapeDataString(yesOrDefaultRedirectURL)}");
            param.Append($"&NoRedirectURL={Uri.EscapeDataString(noRedirectURL)}");
            param.Append($"&Message={Uri.EscapeDataString(message)}");
            param.Append($"&Style=Safe");

            return $"<a class=\"btn btn-success {@class}\" href=\"{basePath}/Utility/ConfirmAction?{param}\">{linkLabel}</a>";
        }

        /// <summary>
        /// Generates a link that navigates via GET to a "confirm action" page where the yes link is YELLOW, but the NO button is still GREEN.
        /// </summary>
        /// <param name="message">The message to be displayed.</param>
        /// <param name="linkLabel">the label for the link that will redirect to this confirm action page.</param>
        /// <param name="controllerURL">The URL which will handle the click of the "yes" or "no" for the confirm action page.</param>
        /// <param name="parameter">An optional parameter to pass to the page and controller function.</param>
        /// <param name="yesOrDefaultRedirectURL">The URL to redirect to AFTER the controller has been called if the user selected YES (or NO, if the NO link is not specified.</param>
        /// <param name="noRedirectURL">The URL to redirect to AFTER the controller has been called if the user selected NO, if not specified, the same link that is provided to yesOrDefaultRedirectURL is used.</param>
        public static string GenerateWarnLink(string basePath, string message, string linkLabel, string controllerURL,
            string? yesOrDefaultRedirectURL, string? noRedirectURL = null, string? @class = "")
        {
            noRedirectURL ??= yesOrDefaultRedirectURL;

            yesOrDefaultRedirectURL.EnsureNotNull();
            noRedirectURL.EnsureNotNull();

            var param = new StringBuilder();
            param.Append($"ControllerURL={Uri.EscapeDataString($"{basePath}{controllerURL}")}");
            param.Append($"&YesRedirectURL={Uri.EscapeDataString(yesOrDefaultRedirectURL)}");
            param.Append($"&NoRedirectURL={Uri.EscapeDataString(noRedirectURL)}");
            param.Append($"&Message={Uri.EscapeDataString(message)}");
            param.Append($"&Style=Warn");

            return $"<a class=\"btn btn-warning {@class}\" href=\"{basePath}/Utility/ConfirmAction?{param}\">{linkLabel}</a>";
        }
    }
}
