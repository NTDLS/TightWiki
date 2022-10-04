using System.Collections.Generic;

namespace SharpWiki.Shared.Models
{
    public class Attachments
    {
        public List<PageFile> Files { get; set; } = new List<PageFile>();
    }
}
