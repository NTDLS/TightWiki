SELECT
	L.Id,
	S.Name as Severity,
	L.[Text],
	L.[ExceptionText],
	L.[StackTrace],
	L.[CreatedDate],

	@PageSize as PaginationPageSize,
	(
		SELECT
			(Count(0) + (@PageSize - 1)) / @PageSize
		FROM
			Log as P
	) as PaginationPageCount
FROM
	Log as L
INNER JOIN Severity as S
	ON L.SeverityId = S.Id
--CUSTOM_ORDER_BEGIN::
--CONFIG::
/*
Id=Id
CreatedDate=[CreatedDate]
*/
--::CONFIG
ORDER BY
	L.Id DESC
--::CUSTOM_ORDER_BEGIN
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize
