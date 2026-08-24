using System;
using System.Collections.Generic;

namespace TightWiki.Data.EfCore._Scaffold.Logging;

public partial class Log
{
    public int Id { get; set; }

    public int? SeverityId { get; set; }

    public string? Text { get; set; }

    public string? ExceptionText { get; set; }

    public string? StackTrace { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual Severity? Severity { get; set; }
}
