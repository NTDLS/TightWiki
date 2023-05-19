namespace TightWiki.Shared.Models.Data
{
    public partial class PageTag
    {
        public int Id { get; set; }
        public int PageId { get; set; }
        public string Tag { get; set; }
    }
}
