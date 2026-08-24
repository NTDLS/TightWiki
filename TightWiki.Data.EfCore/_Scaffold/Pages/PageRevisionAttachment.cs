using System;
using System.Collections.Generic;

namespace TightWiki.Data.EfCore._Scaffold.Pages;

public partial class PageRevisionAttachment
{
    public int PageId { get; set; }

    public int PageFileId { get; set; }

    public int FileRevision { get; set; }

    public int PageRevision { get; set; }

    public virtual Page Page { get; set; } = null!;

    public virtual PageFile PageFile { get; set; } = null!;
}
