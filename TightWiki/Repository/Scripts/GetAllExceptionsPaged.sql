SELECT
	Id,
	[Text],
	[ExceptionText],
	[StackTrace],
	[CreatedDate],

	@PageSize as PaginationPageSize,
	(
		SELECT
			Round(Count(0) / (@PageSize + 0.0)  + 0.999)
		FROM
			[Exception] as P
	) as PaginationPageCount
FROM
	[Exception]
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize
