using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpWiki.Shared.Models.Data
{
    public class PageRevisionHistory
    {
        public int PaginationSize { get; set; }
        public int PaginationCount { get; set; }
        public int PageId { get; set; }
        public string ModifiedByUserName { get; set; }
        public int ModifiedByUserId { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Revision { get; set; }
        public string Navigation { get; set; }
        public int CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; }
        public DateTime CreatedDate { get; set; }
        public string ChangeSummary { get; set; }
    }
}
