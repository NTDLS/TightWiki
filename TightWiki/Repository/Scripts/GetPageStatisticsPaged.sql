SELECT
	MAX(P.Name) as Name,
	MAX(P.Namespace) as Namespace,
	MAX(P.Navigation) as Navigation,
	MAX(Stats.CreatedDate) as LatestBuild,
	COUNT(0) as BuildCount,
	AVG(Stats.WikifyTimeMs) as AvgBuildTimeMs,
	AVG(Stats.MatchCount) as WikiMatches,
	SUM(Stats.ErrorCount) as ErrorCount,
	AVG(Stats.OutgoingLinkCount) as OutgoingLinkCount,
	AVG(Stats.TagCount) as TagCount,
	AVG(Stats.BodySize) as RawBodySize,
	AVG(Stats.ProcessedBodySize) as WikifiedBodySize,
	@PageSize as PaginationPageSize,
	(
		SELECT
			CAST((Count(0) + (@PageSize - 1.0)) / @PageSize AS INTEGER)
		FROM
			[PageStatistics] as Stats
		INNER JOIN pages_db.[Page] as P
			ON P.Id = Stats.PageId
	) as PaginationPageCount
FROM
	[PageStatistics] as Stats
INNER JOIN pages_db.[Page] as P
	ON P.Id = Stats.PageId
GROUP BY
	Stats.PageId
ORDER BY
	MAX(P.Navigation)
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize;
