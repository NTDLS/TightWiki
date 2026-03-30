namespace TightWiki.Plugin.Models
{
    public class TwUpsertEmoji
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Categories { get; set; } = new();
        public byte[]? ImageData { get; set; }
        public string MimeType { get; set; } = string.Empty;
    }
}
