namespace TightWiki.Plugin.Models
{
    public class TwPageSearchToken
    {
        public int PageId { get; set; }
        public double Match { get; set; }
        public double Weight { get; set; }
        public double Score { get; set; }
    }
}
