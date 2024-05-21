namespace TightWiki.Models.ViewModels.Admin
{
    public class ConfirmActionViewModel : ViewModelBase
    {
        public string ActionToConfirm { get; set; } = string.Empty;
        public string PostBackURL { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
