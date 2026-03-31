namespace TightWiki.Plugin.Models
{
    public class TwDatabaseInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public int PageCount { get; set; }
        public int PageSize { get; set; }
        public int DatabaseSize { get; set; }
    }
}
