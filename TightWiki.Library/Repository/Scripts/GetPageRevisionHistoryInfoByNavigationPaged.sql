SELECT
	P.Id as PageId,
	PR.[Name],
	PR.[Description],
	PR.Revision,
	P.Navigation,
	P.CreatedByUserId,
	Createduser.AccountName as CreatedByUserName,
	P.CreatedDate,
	PR.ModifiedByUserId,
	ModifiedUser.AccountName as ModifiedByUserName,
	PR.ModifiedDate,
	@PageSize as PaginationSize,
	(
		SELECT
			Count(0) / (@PageSize + 0.0)
		FROM
			[Page] as P
		INNER JOIN [PageRevision] as PR
			ON PR.PageId = P.Id
		WHERE
			P.Navigation = @Navigation
	) as PaginationCount
FROM
	[Page] as P
INNER JOIN [PageRevision] as PR
	ON PR.PageId = P.Id
INNER JOIN Profile as ModifiedUser
	ON ModifiedUser.UserId = PR.ModifiedByUserId
INNER JOIN Profile as Createduser
	ON Createduser.UserId = P.CreatedByUserId
WHERE
	P.Navigation = @Navigation
ORDER BY
	PR.Revision DESC
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize
