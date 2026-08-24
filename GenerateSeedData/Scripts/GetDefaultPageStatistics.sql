SELECT
	Stats.PageId,
	P.Navigation,
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
	Stats.TotalViewCount
FROM
	PageStatistics as Stats
INNER JOIN pages_db.[Page] as P
	ON P.Id = Stats.PageId;
