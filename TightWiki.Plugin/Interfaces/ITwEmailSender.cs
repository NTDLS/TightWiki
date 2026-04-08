namespace TightWiki.Plugin.Interfaces
{
    /// <summary>
    /// Defines a contract for sending email messages asynchronously.
    /// </summary>
    /// <remarks>Implementations of this interface are responsible for delivering email messages to the
    /// specified recipient. The actual delivery mechanism may vary depending on the implementation. This interface is
    /// typically used to abstract email sending functionality in applications, allowing for easier testing and
    /// configuration.</remarks>
    public interface ITwEmailSender
    {
        /// <summary>
        /// Asynchronously sends an email message to the specified recipient with the given subject and HTML content.
        /// </summary>
        /// <param name="email">The email address of the recipient. Cannot be null or empty.</param>
        /// <param name="subject">The subject line of the email message. Cannot be null or empty.</param>
        /// <param name="htmlMessage">The HTML content to include in the body of the email message. Cannot be null or empty.</param>
        /// <returns>A task that represents the asynchronous send operation.</returns>
        Task SendEmailAsync(string email, string subject, string htmlMessage);
    }
}
