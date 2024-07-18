namespace TightWiki.Models.DataModels
{
    public class PageSearchToken
    {
        public int PageId { get; set; }
        public double Match { get; set; }
        public double Weight { get; set; }
        public double Score { get; set; }
    }
}
