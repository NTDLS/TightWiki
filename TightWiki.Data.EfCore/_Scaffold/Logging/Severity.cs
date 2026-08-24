using System;
using System.Collections.Generic;

namespace TightWiki.Data.EfCore._Scaffold.Logging;

public partial class Severity
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Log> Logs { get; set; } = new List<Log>();
}
