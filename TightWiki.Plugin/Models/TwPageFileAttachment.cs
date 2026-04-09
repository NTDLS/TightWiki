using NTDLS.Helpers;

namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents a file attached to a wiki page, including the full file content,
    /// metadata such as name, size, and MIME type, and pagination support for attachment lists.
    /// </summary>
    public partial class TwPageFileAttachment
    {
        /// <summary>
        /// The number of items per page used when paginating attachment lists.
        /// </summary>
        public int PaginationPageSize { get; set; }

        /// <summary>
        /// The total number of pages available when paginating attachment lists.
        /// </summary>
        public int PaginationPageCount { get; set; }

        /// <summary>
        /// The unique identifier for this file attachment record.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The unique identifier of the page this file is attached to.
        /// </summary>
        public int PageId { get; set; }

        /// <summary>
        /// The original file name of the attachment.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The MIME content type of the attached file.
        /// </summary>
        public string ContentType { get; set; } = string.Empty;

        /// <summary>
        /// The size of the attached file in bytes.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// The date and time this file attachment was uploaded.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// The raw byte content of the attached file.
        /// </summary>
        public byte[] Data { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// The URL-safe navigation path used to locate this file attachment.
        /// </summary>
        public string FileNavigation { get; set; } = string.Empty;

        /// <summary>
        /// The URL-safe navigation path of the page this file is attached to.
        /// </summary>
        public string PageNavigation { get; set; } = string.Empty;

        /// <summary>
        /// The human-readable formatted file size, such as "1.2 MB".
        /// </summary>
        public string FriendlySize => Formatters.FileSize(Size);
    }
}