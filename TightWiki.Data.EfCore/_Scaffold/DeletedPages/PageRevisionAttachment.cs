using System;
using System.Collections.Generic;

namespace TightWiki.Data.EfCore._Scaffold.DeletedPages;

public partial class PageRevisionAttachment
{
    public int PageId { get; set; }

    public int PageFileId { get; set; }

    public int FileRevision { get; set; }

    public int PageRevision { get; set; }
}
