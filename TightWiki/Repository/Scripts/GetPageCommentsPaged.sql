SELECT
	PC.Id,
	PC.PageId,
	PC.CreatedDate,
	PC.UserId,
	PC.Body,
	U.AccountName as UserName,
	U.AccountName as UserNavigation,
	P.[Name] as PageName,
	@PageSize as PaginationSize,
	(
		SELECT
			(Count(0) / (@PageSize + 0.0))
		FROM
			[Page] as iP
		INNER JOIN PageComment as iPC
			ON iPC.PageId = iP.Id
		WHERE
			iP.Navigation = @Navigation
	) as PaginationCount
FROM
	[Page] as P
INNER JOIN PageComment as PC
	ON PC.PageId = P.Id
INNER JOIN Profile as U
	ON U.UserId = PC.UserId
WHERE
	P.Navigation = @Navigation
ORDER BY
	PC.CreatedDate DESC
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize