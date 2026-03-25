SELECT
	TotalViewCount
FROM
	PageStatistics
WHERE
	PageId = @PageId
LIMIT 1;
