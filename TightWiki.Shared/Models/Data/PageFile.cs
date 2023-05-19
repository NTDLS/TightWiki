using System;

namespace TightWiki.Shared.Models.Data
{
    public partial class PageFile
    {
        public int Id { get; set; }
        public int PageId { get; set; }
        public string Name { get; set; }
        public string Navigation { get; set; }
        public int Revision { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
