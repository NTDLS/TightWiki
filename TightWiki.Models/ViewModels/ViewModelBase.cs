namespace TightWiki.Models.ViewModels
{
    public class ViewModelBase
    {
        public string SuccessMessage { get; set; } = string.Empty;
        public string WarningMessage { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
