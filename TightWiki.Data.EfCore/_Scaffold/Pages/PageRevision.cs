using System;
using System.Collections.Generic;

namespace TightWiki.Data.EfCore._Scaffold.Pages;

public partial class PageRevision
{
    public int PageId { get; set; }

    public string Name { get; set; } = null!;

    public string? Namespace { get; set; }

    public string Description { get; set; } = null!;

    public string Body { get; set; } = null!;

    public int Revision { get; set; }

    public string? ChangeSummary { get; set; }

    public Guid ModifiedByUserId { get; set; }

    public DateTime ModifiedDate { get; set; }

    public int DataHash { get; set; }
}
