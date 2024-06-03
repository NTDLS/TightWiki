using System.Text;

namespace TightWiki.Library
{
    public static class ConfirmActionHelper
    {
        /// <summary>
        /// Generates a button that redirects to a "confirm action" page where the yes button is RED.
        /// </summary>
        /// <param name="message">The message to be displayed.</param>
        /// <param name="buttonLabel">the label for the button that will redirect to this confirm action page.</param>
        /// <param name="controllerURL">The URL which will handle the click of the "yes" or "no" for the confirm action page.</param>
        /// <param name="parameter">An optional parameter to pass to the page and controller function.</param>
        /// <param name="yesRedirectURL">The URL to redirect to AFTER the controller has been called if the user selected YES.</param>
        /// <param name="noRedirectURL">The URL to redirect to AFTER the controller has been called if the user selected NO.</param>
        /// <returns></returns>
        public static string GenerateDangerButton(string message, string buttonLabel, string controllerURL,
            string parameter, string yesRedirectURL, string? noRedirectURL)
        {
            yesRedirectURL.EnsureNotNull();

            noRedirectURL.EnsureNotNull();

            var html = new StringBuilder();
            html.Append("<form action='/Utility/ConfirmAction' method='post'>");
            html.Append($"<input type='hidden' name='ControllerURL' value='{controllerURL}' />");
            html.Append($"<input type='hidden' name='YesRedirectURL' value='{yesRedirectURL}' />");
            html.Append($"<input type='hidden' name='NoRedirectURL' value='{noRedirectURL}' />");
            html.Append($"<input type='hidden' name='Message' value='{message}' />");
            html.Append($"<input type='hidden' name='Style' value='Danger' />");
            if (string.IsNullOrEmpty(parameter) == false)
            {
                html.Append($"<input type='hidden' name='Parameter' value='{parameter}' />");
            }
            html.Append($"<button type='submit' class='btn btn-danger rounded-0' name='ActionToConfirm' value='PurgeDeletedPages'>{buttonLabel}</button>");
            html.Append("</form>");

            return html.ToString();
        }

        /// <summary>
        /// Generates a button that redirects to a "confirm action" page where the yes and no buttons are GREEN.
        /// </summary>
        /// <param name="message">The message to be displayed.</param>
        /// <param name="buttonLabel">the label for the button that will redirect to this confirm action page.</param>
        /// <param name="controllerURL">The URL which will handle the click of the "yes" or "no" for the confirm action page.</param>
        /// <param name="parameter">An optional parameter to pass to the page and controller function.</param>
        /// <param name="yesRedirectURL">The URL to redirect to AFTER the controller has been called if the user selected YES.</param>
        /// <param name="noRedirectURL">The URL to redirect to AFTER the controller has been called if the user selected NO.</param>
        public static string GenerateSafeButton(string message, string buttonLabel, string controllerURL,
            string parameter, string? yesRedirectURL, string? noRedirectURL)
        {
            yesRedirectURL.EnsureNotNull();
            noRedirectURL.EnsureNotNull();

            var html = new StringBuilder();

            html.Append("<form action='/Utility/ConfirmAction' method='post'>");
            html.Append($"<input type='hidden' name='ControllerURL' value='{controllerURL}' />");
            html.Append($"<input type='hidden' name='YesRedirectURL' value='{yesRedirectURL}' />");
            html.Append($"<input type='hidden' name='NoRedirectURL' value='{noRedirectURL}' />");
            html.Append($"<input type='hidden' name='Message' value='{message}' />");
            html.Append($"<input type='hidden' name='Style' value='Safe' />");
            if (string.IsNullOrEmpty(parameter) == false)
            {
                html.Append($"<input type='hidden' name='Parameter' value='{parameter}' />");
            }
            html.Append($"<button type='submit' class='btn btn-success rounded-0' name='ActionToConfirm' value='PurgeDeletedPages'>{buttonLabel}</button>");
            html.Append("</form>");

            return html.ToString();
        }

        /// <summary>
        /// Generates a button that redirects to a "confirm action" page where the yes button is YELLOW.
        /// </summary>
        /// <param name="message">The message to be displayed.</param>
        /// <param name="buttonLabel">the label for the button that will redirect to this confirm action page.</param>
        /// <param name="controllerURL">The URL which will handle the click of the "yes" or "no" for the confirm action page.</param>
        /// <param name="parameter">An optional parameter to pass to the page and controller function.</param>
        /// <param name="yesRedirectURL">The URL to redirect to AFTER the controller has been called if the user selected YES.</param>
        /// <param name="noRedirectURL">The URL to redirect to AFTER the controller has been called if the user selected NO.</param>
        public static string GenerateWarnButton(string message, string buttonLabel, string controllerURL,
            string parameter, string? yesRedirectURL, string? noRedirectURL)
        {
            yesRedirectURL.EnsureNotNull();
            noRedirectURL.EnsureNotNull();

            var html = new StringBuilder();

            html.Append("<form action='/Utility/ConfirmAction' method='post'>");
            html.Append($"<input type='hidden' name='ControllerURL' value='{controllerURL}' />");
            html.Append($"<input type='hidden' name='YesRedirectURL' value='{yesRedirectURL}' />");
            html.Append($"<input type='hidden' name='NoRedirectURL' value='{noRedirectURL}' />");
            html.Append($"<input type='hidden' name='Message' value='{message}' />");
            html.Append($"<input type='hidden' name='Style' value='Warn' />");
            if (string.IsNullOrEmpty(parameter) == false)
            {
                html.Append($"<input type='hidden' name='Parameter' value='{parameter}' />");
            }
            html.Append($"<button type='submit' class='btn btn-warning rounded-0' name='ActionToConfirm' value='PurgeDeletedPages'>{buttonLabel}</button>");
            html.Append("</form>");

            return html.ToString();
        }

    }
}
