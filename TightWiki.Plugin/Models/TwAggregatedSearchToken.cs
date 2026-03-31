namespace TightWiki.Plugin.Models
{
    public class TwAggregatedSearchToken
    {
        public string Token { get; set; } = string.Empty;
        public double Weight { get; set; }
        public string DoubleMetaphone { get; set; } = string.Empty;
    }
}
