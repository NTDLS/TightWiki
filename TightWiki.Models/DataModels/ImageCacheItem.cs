namespace TightWiki.Models.DataModels
{
    public partial class ImageCacheItem
    {
        public string ContentType { get; set; } = string.Empty;
        public byte[] Data { get; set; }

        public ImageCacheItem(byte[] bytes, string contentType)
        {
            Data = bytes;
            ContentType = contentType;
        }
    }
}
