INSERT INTO CompilationStatistics
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
    TotalCompilationCount = CompilationStatistics.TotalCompilationCount + 1,
    LastWikifyTimeMs = @LastWikifyTimeMs,
    TotalWikifyTimeMs = CompilationStatistics.TotalWikifyTimeMs + @LastWikifyTimeMs,
    LastMatchCount = @LastMatchCount,
    LastErrorCount = @LastErrorCount,
    LastOutgoingLinkCount = @LastOutgoingLinkCount,
    LastTagCount = @LastTagCount,
    LastProcessedBodySize = @LastProcessedBodySize,
    LastBodySize = @LastBodySize;
