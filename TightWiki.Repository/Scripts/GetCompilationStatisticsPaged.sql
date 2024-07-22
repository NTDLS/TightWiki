SELECT
	MAX(P.Name) as Name,
	MAX(P.Namespace) as Namespace,
	MAX(P.Navigation) as Navigation,
	MAX(Stats.CreatedDate) as LatestBuild,
	COUNT(0) as Compilations,
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
--CUSTOM_ORDER_BEGIN::
--CONFIG::
/*
Name=MAX(P.Name)
Namespace=MAX(P.Namespace)
Navigation=MAX(P.Navigation)
CreatedDate=MAX(P.CreatedDate)
Compilations=COUNT(0)
AvgBuildTimeMs=AVG(Stats.WikifyTimeMs)
AvgWikiMatches=AVG(Stats.MatchCount)
TotalErrorCount=SUM(Stats.ErrorCount)
AvgOutgoingLinkCount=AVG(Stats.OutgoingLinkCount)
AvgTagCount=AVG(Stats.TagCount)
AvgRawBodySize=AVG(Stats.BodySize)
AvgWikifiedBodySize=AVG(Stats.ProcessedBodySize)
*/
--::CONFIG
ORDER BY
	MAX(P.Name) DESC
--::CUSTOM_ORDER_BEGIN
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize;
