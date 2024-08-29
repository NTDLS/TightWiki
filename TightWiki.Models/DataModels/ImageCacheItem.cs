namespace TightWiki.Models.DataModels
{
    public partial class ImageCacheItem
    {
        public string ContentType { get; set; } = string.Empty;
        public byte[] Bytes { get; set; }

        public ImageCacheItem(byte[] bytes, string contentType)
        {
            Bytes = bytes;
            ContentType = contentType;
        }
    }
}
