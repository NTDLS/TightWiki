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
	@PageSize as PaginationPageSize,
	(
		SELECT
			Round(Count(0) / (@PageSize + 0.0)  + 0.999)
		FROM
			[Page] as P
		INNER JOIN [PageRevision] as PR
			ON PR.PageId = P.Id
		WHERE
			P.Navigation = @Navigation
	) as PaginationPageCount
FROM
	[Page] as P
INNER JOIN [PageRevision] as PR
	ON PR.PageId = P.Id
INNER JOIN users_db.Profile as ModifiedUser
	ON ModifiedUser.UserId = PR.ModifiedByUserId
INNER JOIN users_db.Profile as Createduser
	ON Createduser.UserId = P.CreatedByUserId
WHERE
	P.Navigation = @Navigation
ORDER BY
	PR.Revision DESC
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize
