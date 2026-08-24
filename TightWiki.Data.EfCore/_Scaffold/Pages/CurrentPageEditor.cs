using System;
using System.Collections.Generic;

namespace TightWiki.Data.EfCore._Scaffold.Pages;

public partial class CurrentPageEditor
{
    public int PageId { get; set; }

    public int UserId { get; set; }

    public string? AccountName { get; set; }

    public string? Utcdate { get; set; }
}
