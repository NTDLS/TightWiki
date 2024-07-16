namespace TightWiki.Models.ViewModels.Utility
{
    public class NotifyViewModel : ViewModelBase
    {
        public string RedirectURL { get; set; } = string.Empty;
        public int RedirectTimeout { get; set; }
    }
}
