namespace TightWiki.Plugin.Interfaces
{
    public interface IWikiEmailSender
    {
        Task SendEmailAsync(string email, string subject, string htmlMessage);
    }
}
