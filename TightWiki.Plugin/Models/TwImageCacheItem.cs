namespace TightWiki.Plugin.Models
{
    public partial class TwImageCacheItem
    {
        public string ContentType { get; set; } = string.Empty;
        public byte[] Bytes { get; set; }

        public TwImageCacheItem(byte[] bytes, string contentType)
        {
            Bytes = bytes;
            ContentType = contentType;
        }
    }
}
