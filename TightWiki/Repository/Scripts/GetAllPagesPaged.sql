SELECT
	P.Id,
	P.[Name],
	P.Navigation,
	P.[Description],
	P.Revision,
	P.CreatedByUserId,
	P.CreatedDate,
	P.ModifiedByUserId,
	P.ModifiedDate,
	Createduser.AccountName as CreatedByUserName,
	ModifiedUser.AccountName as ModifiedByUserName,
	@PageSize as PaginationPageSize,
	(
		SELECT
			Round(Count(0) / (@PageSize + 0.0)  + 0.999)
		FROM
			[Page] as P
	) as PaginationPageCount
FROM
	[Page] as P
INNER JOIN users_db.Profile as ModifiedUser
	ON ModifiedUser.UserId = P.ModifiedByUserId
INNER JOIN users_db.Profile as Createduser
	ON Createduser.UserId = P.CreatedByUserId
ORDER BY
	P.[Name],
	P.Id
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize
