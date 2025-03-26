SELECT
	S.Id,
	S.[Name],
	S.[Description],
	@PageSize as PaginationPageSize,
	(
		SELECT
			CAST((Count(0) + (@PageSize - 1.0)) / @PageSize AS INTEGER)
		FROM
			[SecurityGroup] as P
	) as PaginationPageCount
FROM
	[SecurityGroup] as S
--CUSTOM_ORDER_BY>>
--CONFIG>>
/*
Id=S.Id
Name=S.Name
Description=S.Description
*/
--<<CONFIG
ORDER BY
	S.Name
--<<CUSTOM_ORDER_BY
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize
