namespace TightWiki.RequestModels
{
    public class PagePreviewRequest
    {
        public string Body { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string PageNavigation { get; set; } = string.Empty;
    }
}
