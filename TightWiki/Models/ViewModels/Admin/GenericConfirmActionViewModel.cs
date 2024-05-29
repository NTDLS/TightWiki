namespace TightWiki.Models.ViewModels.Admin
{
    public class GenericConfirmActionViewModel : ViewModelBase
    {
        public string ActionToConfirm { get; set; } = string.Empty;
        public string PostBackURL { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
