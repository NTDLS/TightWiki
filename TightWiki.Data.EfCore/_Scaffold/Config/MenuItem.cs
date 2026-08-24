using System;
using System.Collections.Generic;

namespace TightWiki.Data.EfCore._Scaffold.Config;

public partial class MenuItem
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Link { get; set; } = null!;

    public int Ordinal { get; set; }
}
