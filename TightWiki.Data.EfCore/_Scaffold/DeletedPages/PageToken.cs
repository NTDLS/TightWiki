using System;
using System.Collections.Generic;

namespace TightWiki.Data.EfCore._Scaffold.DeletedPages;

public partial class PageToken
{
    public int PageId { get; set; }

    public string Token { get; set; } = null!;

    public double Weight { get; set; }

    public string DoubleMetaphone { get; set; } = null!;
}
