namespace TightWiki.Models.DataModels
{
    public class Emoji
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Shortcut { get; set; } = string.Empty;
        public int PaginationCount { get; set; }
        public string Categories { get; set; } = string.Empty;
        public byte[]? ImageData { get; set; }
        public string MimeType { get; set; } = string.Empty;
    }
}
