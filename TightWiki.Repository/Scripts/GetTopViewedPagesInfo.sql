SELECT
	P.Id,
	P.[Name],
	P.[Description],
	P.[Revision],
	P.Navigation,
	P.CreatedByUserId,
	P.CreatedDate,
	P.ModifiedByUserId,
	IFNULL(P.ModifiedDate, P.CreatedDate) as ModifiedDate,
	Stats.TotalViewCount
FROM
	statistics_db.PageStatistics as Stats
INNER JOIN [Page] as P
	ON P.Id = Stats.PageId
ORDER BY
	Stats.TotalViewCount DESC,
	P.[Name] ASC
LIMIT @TopCount;
