using System.Collections.Generic;
using TightWiki.Shared.Models.Data;

namespace TightWiki.Shared.Models.View
{
    public class FileAttachmentModel : ModelBase
    {
        public List<PageFileAttachment> Files { get; set; } = new List<PageFileAttachment>();
    }
}
