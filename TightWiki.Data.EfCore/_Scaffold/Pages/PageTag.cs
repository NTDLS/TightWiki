using System;
using System.Collections.Generic;

namespace TightWiki.Data.EfCore._Scaffold.Pages;

public partial class PageTag
{
    public int PageId { get; set; }

    public string Tag { get; set; } = null!;

    public string Navigation { get; set; } = null!;

    public virtual Page Page { get; set; } = null!;
}
