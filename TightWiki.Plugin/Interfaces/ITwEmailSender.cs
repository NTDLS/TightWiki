namespace TightWiki.Plugin.Interfaces
{
    public interface ITwEmailSender
    {
        Task SendEmailAsync(string email, string subject, string htmlMessage);
    }
}
