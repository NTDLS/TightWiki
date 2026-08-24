using System;
using System.Collections.Generic;

namespace TightWiki.Data.EfCore._Scaffold.Config;

public partial class VersionState
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Value { get; set; } = null!;
}
