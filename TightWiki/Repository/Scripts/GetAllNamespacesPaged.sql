SELECT
	P.[Namespace],
	Count(0) as [CountOfPages],

	@PageSize as PaginationPageSize,
	(
		SELECT
			Count(DISTINCT P.[Namespace]) / (@PageSize + 0.0)
		FROM
			[Page] as P
	) as PaginationPageCount
FROM
	[Page] as P
GROUP BY
	[Namespace]
ORDER BY
	P.[Namespace]
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize
