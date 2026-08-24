using System;
using System.Collections.Generic;

namespace TightWiki.Data.EfCore._Scaffold.Config;

public partial class ConfigurationGroup
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }
}
