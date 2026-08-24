using System;
using System.Collections.Generic;

namespace TightWiki.Data.EfCore._Scaffold.Pages;

public partial class PageFile
{
    public int Id { get; set; }

    public int PageId { get; set; }

    public string Name { get; set; } = null!;

    public string Navigation { get; set; } = null!;

    public int Revision { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual Page Page { get; set; } = null!;

    public virtual ICollection<PageFileRevision> PageFileRevisions { get; set; } = new List<PageFileRevision>();

    public virtual ICollection<PageRevisionAttachment> PageRevisionAttachments { get; set; } = new List<PageRevisionAttachment>();
}
