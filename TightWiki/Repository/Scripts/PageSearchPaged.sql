SELECT
	P.Id,
	(ST.[Score] / @MaximumScore) * 100.0 as Score,
	ST.[Match],
	ST.[Weight],
	P.[Name],
	P.Navigation,
	P.[Description],
	P.Revision,
	P.CreatedByUserId,
	P.CreatedDate,
	P.ModifiedByUserId,
	P.ModifiedDate,
	Coalesce(Createduser.AccountName, '') as CreatedByUserName,
	Coalesce(ModifiedUser.AccountName, '') as ModifiedByUserName,
	@PageSize as PaginationSize,
	(
		SELECT
			Round(Count(0) / (@PageSize + 0.0)  + 0.999)
		FROM
			[Page] as P
		INNER JOIN TempSearchTerms as ST
			ON ST.PageId = P.Id
	) as PaginationCount
FROM
	[Page] as P
LEFT OUTER JOIN users_db.Profile as ModifiedUser
	ON ModifiedUser.UserId = P.ModifiedByUserId
LEFT OUTER JOIN users_db.Profile as Createduser
	ON Createduser.UserId = P.CreatedByUserId
INNER JOIN TempSearchTerms as ST
	ON ST.PageId = P.Id
ORDER BY
	ST.[Score] DESC,
	P.[Name],
	P.Id
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize
