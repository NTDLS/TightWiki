INSERT INTO PageStatistics
(
    PageId,
    LastCompileDateTime,
    TotalCompilationCount,
    TotalViewCount
)
VALUES
(
    @PageId,
    @LastCompileDateTime,
    1,
    1
)
ON CONFLICT(PageId) DO UPDATE SET
    TotalViewCount = PageStatistics.TotalViewCount + 1,
