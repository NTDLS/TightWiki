namespace TightWiki.Shared.Models.Data
{
    public class Emoji
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Shortcut { get; set; }
        public int PaginationCount { get; set; }
        public string Categories { get; set; }
        public byte[] ImageData { get; set; }
    }
}
