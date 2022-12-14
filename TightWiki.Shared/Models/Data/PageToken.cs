namespace TightWiki.Shared.Models.Data
{
    public class PageToken
    {
        public int PageId { get; set; }
        public string Token { get; set; }
        public string DoubleMetaphone { get; set; }
        public double Weight { get; set; }
    }
}
