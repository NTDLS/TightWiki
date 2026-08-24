using System;
using System.Collections.Generic;

namespace TightWiki.Data.EfCore._Scaffold.Pages;

public partial class PageReference
{
    public int PageId { get; set; }

    public string ReferencesPageName { get; set; } = null!;

    public string ReferencesPageNavigation { get; set; } = null!;

    public int? ReferencesPageId { get; set; }

    public virtual Page Page { get; set; } = null!;

    public virtual Page? ReferencesPage { get; set; }
}
