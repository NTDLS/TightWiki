INSERT INTO PageStatistics
(
    PageId,
    LastCompileDateTime,
    TotalCompilationCount,
    LastWikifyTimeMs,
    TotalWikifyTimeMs,
    LastMatchCount,
    LastErrorCount,
    LastOutgoingLinkCount,
    LastTagCount,
    LastProcessedBodySize,
    LastBodySize
)
VALUES
(
    @PageId,
    @LastCompileDateTime,
    1,
    @LastWikifyTimeMs,
    @LastWikifyTimeMs,
    @LastMatchCount,
    @LastErrorCount,
    @LastOutgoingLinkCount,
    @LastTagCount,
    @LastProcessedBodySize,
    @LastBodySize
)
ON CONFLICT(PageId) DO UPDATE SET
    LastCompileDateTime = @LastCompileDateTime,
    TotalCompilationCount = PageStatistics.TotalCompilationCount + 1,
    LastWikifyTimeMs = @LastWikifyTimeMs,
    TotalWikifyTimeMs = PageStatistics.TotalWikifyTimeMs + @LastWikifyTimeMs,
    LastMatchCount = @LastMatchCount,
    LastErrorCount = @LastErrorCount,
    LastOutgoingLinkCount = @LastOutgoingLinkCount,
    LastTagCount = @LastTagCount,
    LastProcessedBodySize = @LastProcessedBodySize,
    LastBodySize = @LastBodySize;
