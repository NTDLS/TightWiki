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
        /// <returns></returns>
        public static string GenerateDangerLink(string basePath, string message, string linkLabel, string controllerURL,
            string? yesOrDefaultRedirectURL, string? noRedirectURL = null)
        {
            noRedirectURL ??= yesOrDefaultRedirectURL;

            yesOrDefaultRedirectURL.EnsureNotNull();
            noRedirectURL.EnsureNotNull();

            var param = new StringBuilder();
            param.Append($"ControllerURL={Uri.EscapeDataString(controllerURL)}");
            param.Append($"&YesRedirectURL={Uri.EscapeDataString(yesOrDefaultRedirectURL)}");
            param.Append($"&NoRedirectURL={Uri.EscapeDataString(noRedirectURL)}");
            param.Append($"&Message={Uri.EscapeDataString(message)}");
            param.Append($"&Style=Danger");

            return $"<a class=\"btn btn-danger btn-thin\" href=\"{basePath}/Utility/ConfirmAction?{param}\">{linkLabel}</a>";
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
        /// <returns></returns>
        public static string GenerateSafeLink(string basePath, string message, string linkLabel, string controllerURL,
            string? yesOrDefaultRedirectURL, string? noRedirectURL = null)
        {
            noRedirectURL ??= yesOrDefaultRedirectURL;

            yesOrDefaultRedirectURL.EnsureNotNull();
            noRedirectURL.EnsureNotNull();

            var param = new StringBuilder();
            param.Append($"ControllerURL={Uri.EscapeDataString(controllerURL)}");
            param.Append($"&YesRedirectURL={Uri.EscapeDataString(yesOrDefaultRedirectURL)}");
            param.Append($"&NoRedirectURL={Uri.EscapeDataString(noRedirectURL)}");
            param.Append($"&Message={Uri.EscapeDataString(message)}");
            param.Append($"&Style=Safe");

            return $"<a class=\"btn btn-success btn-thin\" href=\"{basePath}/Utility/ConfirmAction?{param}\">{linkLabel}</a>";
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
        /// <returns></returns>
        public static string GenerateWarnLink(string basePath, string message, string linkLabel, string controllerURL,
            string? yesOrDefaultRedirectURL, string? noRedirectURL = null)
        {
            noRedirectURL ??= yesOrDefaultRedirectURL;

            yesOrDefaultRedirectURL.EnsureNotNull();
            noRedirectURL.EnsureNotNull();

            var param = new StringBuilder();
            param.Append($"ControllerURL={Uri.EscapeDataString(controllerURL)}");
            param.Append($"&YesRedirectURL={Uri.EscapeDataString(yesOrDefaultRedirectURL)}");
            param.Append($"&NoRedirectURL={Uri.EscapeDataString(noRedirectURL)}");
            param.Append($"&Message={Uri.EscapeDataString(message)}");
            param.Append($"&Style=Warn");

            return $"<a class=\"btn btn-warning btn-thin\" href=\"{basePath}/Utility/ConfirmAction?{param}\">{linkLabel}</a>";
        }

        /*
        /// <summary>
        /// Generates a link that navigates via POST to a "confirm action" page where the yes button is RED, but the NO button is still GREEN.
        /// </summary>
        /// <param name="message">The message to be displayed.</param>
        /// <param name="buttonLabel">the label for the button that will redirect to this confirm action page.</param>
        /// <param name="controllerURL">The URL which will handle the click of the "yes" or "no" for the confirm action page.</param>
        /// <param name="parameter">An optional parameter to pass to the page and controller function.</param>
        /// <param name="yesOrDefaultRedirectURL">The URL to redirect to AFTER the controller has been called if the user selected YES (or NO, if the NO link is not specified.</param>
        /// <param name="noRedirectURL">The URL to redirect to AFTER the controller has been called if the user selected NO, if not specified, the same link that is provided to yesOrDefaultRedirectURL is used.</param>
        /// <returns></returns>
        public static string GenerateDangerButton(string message, string buttonLabel, string controllerURL,
            string? yesOrDefaultRedirectURL, string? noRedirectURL = null)
        {
            noRedirectURL ??= yesOrDefaultRedirectURL;

            yesOrDefaultRedirectURL.EnsureNotNull();
            noRedirectURL.EnsureNotNull();

            var html = new StringBuilder();
            html.Append("<form action='/Utility/ConfirmAction' method='post'>");
            html.Append($"<input type='hidden' name='ControllerURL' value='{controllerURL}' />");
            html.Append($"<input type='hidden' name='YesRedirectURL' value='{yesOrDefaultRedirectURL}' />");
            html.Append($"<input type='hidden' name='NoRedirectURL' value='{noRedirectURL}' />");
            html.Append($"<input type='hidden' name='Message' value='{message}' />");
            html.Append($"<input type='hidden' name='Style' value='Danger' />");
            html.Append($"<button type='submit' class='btn btn-danger rounded-0' name='ActionToConfirm' value='PurgeDeletedPages'>{buttonLabel}</button>");
            html.Append("</form>");

            return html.ToString();
        }

        /// <summary>
        /// Generates a link that navigates via POST to a "confirm action" page where the yes and no buttons are GREEN.
        /// </summary>
        /// <param name="message">The message to be displayed.</param>
        /// <param name="buttonLabel">the label for the button that will redirect to this confirm action page.</param>
        /// <param name="controllerURL">The URL which will handle the click of the "yes" or "no" for the confirm action page.</param>
        /// <param name="parameter">An optional parameter to pass to the page and controller function.</param>
        /// <param name="yesOrDefaultRedirectURL">The URL to redirect to AFTER the controller has been called if the user selected YES (or NO, if the NO link is not specified.</param>
        /// <param name="noRedirectURL">The URL to redirect to AFTER the controller has been called if the user selected NO, if not specified, the same link that is provided to yesOrDefaultRedirectURL is used.</param>
        public static string GenerateSafeButton(string message, string buttonLabel, string controllerURL,
            string? yesOrDefaultRedirectURL, string? noRedirectURL = null)
        {
            noRedirectURL ??= yesOrDefaultRedirectURL;

            yesOrDefaultRedirectURL.EnsureNotNull();
            noRedirectURL.EnsureNotNull();

            var html = new StringBuilder();

            html.Append("<form action='/Utility/ConfirmAction' method='post'>");
            html.Append($"<input type='hidden' name='ControllerURL' value='{controllerURL}' />");
            html.Append($"<input type='hidden' name='YesRedirectURL' value='{yesOrDefaultRedirectURL}' />");
            html.Append($"<input type='hidden' name='NoRedirectURL' value='{noRedirectURL}' />");
            html.Append($"<input type='hidden' name='Message' value='{message}' />");
            html.Append($"<input type='hidden' name='Style' value='Safe' />");
            html.Append($"<button type='submit' class='btn btn-success rounded-0' name='ActionToConfirm' value='PurgeDeletedPages'>{buttonLabel}</button>");
            html.Append("</form>");

            return html.ToString();
        }

        /// <summary>
        /// Generates a link that navigates via POST to a "confirm action" page where the yes button is YELLOW, but the NO button is still GREEN.
        /// </summary>
        /// <param name="message">The message to be displayed.</param>
        /// <param name="buttonLabel">the label for the button that will redirect to this confirm action page.</param>
        /// <param name="controllerURL">The URL which will handle the click of the "yes" or "no" for the confirm action page.</param>
        /// <param name="parameter">An optional parameter to pass to the page and controller function.</param>
        /// <param name="yesOrDefaultRedirectURL">The URL to redirect to AFTER the controller has been called if the user selected YES (or NO, if the NO link is not specified.</param>
        /// <param name="noRedirectURL">The URL to redirect to AFTER the controller has been called if the user selected NO, if not specified, the same link that is provided to yesOrDefaultRedirectURL is used.</param>
        public static string GenerateWarnButton(string message, string buttonLabel, string controllerURL,
            string? yesOrDefaultRedirectURL, string? noRedirectURL = null)
        {
            noRedirectURL ??= yesOrDefaultRedirectURL;

            yesOrDefaultRedirectURL.EnsureNotNull();
            noRedirectURL.EnsureNotNull();

            var html = new StringBuilder();

            html.Append("<form action='/Utility/ConfirmAction' method='post'>");
            html.Append($"<input type='hidden' name='ControllerURL' value='{controllerURL}' />");
            html.Append($"<input type='hidden' name='YesRedirectURL' value='{yesOrDefaultRedirectURL}' />");
            html.Append($"<input type='hidden' name='NoRedirectURL' value='{noRedirectURL}' />");
            html.Append($"<input type='hidden' name='Message' value='{message}' />");
            html.Append($"<input type='hidden' name='Style' value='Warn' />");
            html.Append($"<button type='submit' class='btn btn-warning rounded-0' name='ActionToConfirm' value='PurgeDeletedPages'>{buttonLabel}</button>");
            html.Append("</form>");

            return html.ToString();
        }
        */
    }
}
