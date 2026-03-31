namespace TightWiki.Plugin.Models
{
    public partial class TwPageTag
    {
        public int Id { get; set; }
        public int PageId { get; set; }
        public string Tag { get; set; } = string.Empty;
    }
}
