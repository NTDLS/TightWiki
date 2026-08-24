using System;
using System.Collections.Generic;

namespace TightWiki.Data.EfCore._Scaffold.DeletedPages;

public partial class PageFile
{
    public int Id { get; set; }

    public int PageId { get; set; }

    public string Name { get; set; } = null!;

    public string Navigation { get; set; } = null!;

    public int Revision { get; set; }

    public string CreatedDate { get; set; } = null!;
}
