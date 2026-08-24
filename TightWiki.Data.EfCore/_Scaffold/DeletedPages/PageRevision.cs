using System;
using System.Collections.Generic;

namespace TightWiki.Data.EfCore._Scaffold.DeletedPages;

public partial class PageRevision
{
    public int PageId { get; set; }

    public string Name { get; set; } = null!;

    public string? Namespace { get; set; }

    public string Description { get; set; } = null!;

    public string Body { get; set; } = null!;

    public int Revision { get; set; }

    public string? ChangeSummary { get; set; }

    public string ModifiedByUserId { get; set; } = null!;

    public string ModifiedDate { get; set; } = null!;

    public int DataHash { get; set; }
}
