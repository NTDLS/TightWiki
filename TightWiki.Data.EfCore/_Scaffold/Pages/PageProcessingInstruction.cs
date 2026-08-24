using System;
using System.Collections.Generic;

namespace TightWiki.Data.EfCore._Scaffold.Pages;

public partial class PageProcessingInstruction
{
    public int PageId { get; set; }

    public string Instruction { get; set; } = null!;

    public virtual Page Page { get; set; } = null!;
}
