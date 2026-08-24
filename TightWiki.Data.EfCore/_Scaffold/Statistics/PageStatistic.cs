using System;
using System.Collections.Generic;

namespace TightWiki.Data.EfCore._Scaffold.Statistics;

public partial class PageStatistic
{
    public int Id { get; set; }

    public int PageId { get; set; }

    public DateTime LastCompileDateTime { get; set; }

    public int TotalCompilationCount { get; set; }

    public double? LastWikifyTimeMs { get; set; }

    public double? TotalWikifyTimeMs { get; set; }

    public int? LastMatchCount { get; set; }

    public int? LastErrorCount { get; set; }

    public int? LastOutgoingLinkCount { get; set; }

    public int? LastTagCount { get; set; }

    public int? LastProcessedBodySize { get; set; }

    public int? LastBodySize { get; set; }

    public int TotalViewCount { get; set; }
}
