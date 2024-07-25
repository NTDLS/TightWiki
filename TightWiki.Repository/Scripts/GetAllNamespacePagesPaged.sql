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
			CAST((Count(0) + (@PageSize - 1.0)) / @PageSize AS INTEGER)
		FROM
			[Page] as P
		WHERE
			P.[Namespace] = @Namespace
	) as PaginationPageCount
FROM
	[Page] as P
LEFT OUTER JOIN users_db.Profile as ModifiedUser
	ON ModifiedUser.UserId = P.ModifiedByUserId
LEFT OUTER JOIN users_db.Profile as Createduser
	ON Createduser.UserId = P.CreatedByUserId
WHERE
	P.[Namespace] = @Namespace
--CUSTOM_ORDER_BEGIN::
--CONFIG::
/*
Name=P.[Name]
Revision=P.Revision
ModifiedBy=ModifiedUser.AccountName
ModifiedDate=P.ModifiedDate
*/
--::CONFIG
ORDER BY
	P.[Name]
--::CUSTOM_ORDER_BEGIN
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize
