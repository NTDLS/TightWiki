namespace TightWiki.Library
{
    public interface IWikiEmailSender
    {
        Task SendEmailAsync(string email, string subject, string htmlMessage);
    }
}
