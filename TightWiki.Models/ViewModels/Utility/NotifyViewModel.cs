namespace TightWiki.Models.ViewModels.Utility
{
    public class NotifyViewModel : ViewModelBase
    {
        public string NotifySuccessMessage { get; set; } = string.Empty;
        public string NotifyWarningMessage { get; set; } = string.Empty;
        public string NotifyErrorMessage { get; set; } = string.Empty;
        public string RedirectURL { get; set; } = string.Empty;
        public int RedirectTimeout { get; set; }
    }
}
