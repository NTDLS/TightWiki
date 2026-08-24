using System;
using System.Collections.Generic;

namespace TightWiki.Data.EfCore._Scaffold.DeletedPages;

public partial class Page
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Namespace { get; set; } = null!;

    public string Navigation { get; set; } = null!;

    public string Description { get; set; } = null!;

    public int Revision { get; set; }

    public string CreatedByUserId { get; set; } = null!;

    public string CreatedDate { get; set; } = null!;

    public string ModifiedByUserId { get; set; } = null!;

    public string ModifiedDate { get; set; } = null!;
}
