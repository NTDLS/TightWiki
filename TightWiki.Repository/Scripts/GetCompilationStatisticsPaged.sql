SELECT
	MAX(P.Name) as PageName,
	MAX(P.Namespace) as Namespace,
	MAX(P.Navigation) as Navigation,
    Stats.PageId,
    Stats.LastCompileDateTime,
    Stats.TotalCompilationCount,
    Stats.LastWikifyTimeMs,
    Stats.TotalWikifyTimeMs,
    Stats.LastMatchCount,
    Stats.LastErrorCount,
    Stats.LastOutgoingLinkCount,
    Stats.LastTagCount,
    Stats.LastProcessedBodySize,
    Stats.LastBodySize,
	@PageSize as PaginationPageSize,
	(
		SELECT
			(Count(DISTINCT P.Id) + (@PageSize - 1)) / @PageSize
		FROM
			CompilationStatistics as Stats
		INNER JOIN pages_db.[Page] as P
			ON P.Id = Stats.PageId
	) as PaginationPageCount
FROM
	CompilationStatistics as Stats
INNER JOIN pages_db.[Page] as P
	ON P.Id = Stats.PageId
GROUP BY
	Stats.PageId
--CUSTOM_ORDER_BEGIN::
--CONFIG::
/*
Name=MAX(P.Name)
Navigation=MAX(P.Navigation)
PageId=Stats.PageId
LastCompileDateTime=Stats.LastCompileDateTime
TotalCompilationCount=Stats.TotalCompilationCount
LastWikifyTimeMs=Stats.LastWikifyTimeMs
TotalWikifyTimeMs=Stats.TotalWikifyTimeMs
LastMatchCount=Stats.LastMatchCount
LastErrorCount=Stats.LastErrorCount
LastOutgoingLinkCount=Stats.LastOutgoingLinkCount
LastTagCount=Stats.LastTagCount
LastProcessedBodySize=Stats.LastProcessedBodySize
LastBodySize=Stats.LastBodySize
*/
--::CONFIG
ORDER BY
	MAX(P.Name)
--::CUSTOM_ORDER_BEGIN
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize
