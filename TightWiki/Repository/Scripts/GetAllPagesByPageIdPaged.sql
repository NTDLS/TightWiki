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
	@PageSize as PaginationSize,
	(
		SELECT
			Round(Count(0) / (@PageSize + 0.0)  + 0.999)
		FROM
			[Page] as P
		WHERE
			P.Id IN (SELECT PID.Value FROM TempPageIds as PID)
	) as PaginationCount
FROM
	[Page] as P
INNER JOIN users_db.Profile as ModifiedUser
	ON ModifiedUser.UserId = P.ModifiedByUserId
INNER JOIN users_db.Profile as Createduser
	ON Createduser.UserId = P.CreatedByUserId
WHERE
	P.Id IN (SELECT PID.Value FROM TempPageIds as PID)
ORDER BY
	P.[Name],
	P.Id
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize
