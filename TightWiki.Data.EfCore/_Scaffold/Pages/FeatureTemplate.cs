using System;
using System.Collections.Generic;

namespace TightWiki.Data.EfCore._Scaffold.Pages;

public partial class FeatureTemplate
{
    public string Name { get; set; } = null!;

    public string Type { get; set; } = null!;

    public int? PageId { get; set; }

    public string? Description { get; set; }

    public string? TemplateText { get; set; }

    public virtual Page? Page { get; set; }
}
