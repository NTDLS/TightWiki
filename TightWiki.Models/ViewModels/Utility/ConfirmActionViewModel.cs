namespace TightWiki.Models.ViewModels.Utility
{
    public class ConfirmActionViewModel : ViewModelBase
    {
        public string ControllerURL { get; set; } = string.Empty;
        public string YesRedirectURL { get; set; } = string.Empty;
        public string NoRedirectURL { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Style { get; set; } = string.Empty;
        public string? Parameter { get; set; } = string.Empty;
        public bool UserSelection { get; set; }
    }
}
