SELECT
	MAX(P.Name) as Name,
	MAX(P.Namespace) as Namespace,
	MAX(P.Navigation) as Navigation,
	MAX(Stats.CreatedDate) as LatestBuild,
	COUNT(0) as BuildCount,
	AVG(Stats.WikifyTimeMs) as AvgBuildTimeMs,
	AVG(Stats.MatchCount) as AvgWikiMatches,
	SUM(Stats.ErrorCount) as TotalErrorCount,
	AVG(Stats.OutgoingLinkCount) as AvgOutgoingLinkCount,
	AVG(Stats.TagCount) as AvgTagCount,
	AVG(Stats.BodySize) as AvgRawBodySize,
	AVG(Stats.ProcessedBodySize) as AvgWikifiedBodySize,
	@PageSize as PaginationPageSize,
	(
		SELECT
			CAST((Count(DISTINCT P.Id) + (@PageSize - 1.0)) / @PageSize AS INTEGER)
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
ORDER BY
	MAX(P.Navigation)
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize;
