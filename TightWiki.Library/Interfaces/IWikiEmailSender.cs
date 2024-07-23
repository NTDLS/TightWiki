namespace TightWiki.Library.Interfaces
{
    public interface IWikiEmailSender
    {
        Task SendEmailAsync(string email, string subject, string htmlMessage);
    }
}
