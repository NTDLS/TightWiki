using System;
using System.Collections.Generic;

namespace TightWiki.Data.EfCore._Scaffold.DeletedPages;

public partial class PageTag
{
    public int PageId { get; set; }

    public string Tag { get; set; } = null!;
}
