namespace TightWiki.Shared.Models.View
{
    public class ConfirmActionModel : ModelBase
    {
        public string ActionToConfirm { get; set; }
        public string PostBackURL { get; set; }
        public string Message { get; set; }
    }
}
