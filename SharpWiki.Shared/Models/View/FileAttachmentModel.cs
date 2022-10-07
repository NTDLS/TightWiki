using SharpWiki.Shared.Models.Data;
using System.Collections.Generic;

namespace SharpWiki.Shared.Models.View
{
    public class FileAttachmentModel
    {
        public List<PageFileAttachment> Files { get; set; } = new List<PageFileAttachment>();
    }
}
