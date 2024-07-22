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
--CUSTOM_ORDER_BEGIN::
--CONFIG::
/*
Name=P.[Namespace]
Pages=Count(0)
*/
--::CONFIG
ORDER BY
	P.[Namespace]
--::CUSTOM_ORDER_BEGIN
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize
