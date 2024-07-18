namespace TightWiki.Models.DataModels
{
    public class UpsertEmoji
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Categories { get; set; } = new List<string>();
        public byte[]? ImageData { get; set; }
        public string MimeType { get; set; } = string.Empty;
    }
}
