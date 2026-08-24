using System;
using System.Collections.Generic;

namespace TightWiki.Data.EfCore._Scaffold.Config;

public partial class ConfigurationEntry
{
    public int Id { get; set; }

    public int ConfigurationGroupId { get; set; }

    public string Name { get; set; } = null!;

    public string? Value { get; set; }

    public int DataTypeId { get; set; }

    public string? Description { get; set; }

    public int IsEncrypted { get; set; }

    public int IsRequired { get; set; }
}
