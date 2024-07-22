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
	DeletedUser.AccountName as DeletedByUserName,
	DM.DeletedDate,
	@PageSize as PaginationPageSize,
	(
		SELECT
			CAST((Count(0) + (@PageSize - 1.0)) / @PageSize AS INTEGER)
		FROM
			[Page] as P
		WHERE
			P.Id IN (SELECT PID.Value FROM TempPageIds as PID)
	) as PaginationPageCount
FROM
	[Page] as P
INNER JOIN DeletionMeta as DM
	ON DM.PageId = P.Id
INNER JOIN users_db.Profile as ModifiedUser
	ON ModifiedUser.UserId = P.ModifiedByUserId
INNER JOIN users_db.Profile as Createduser
	ON Createduser.UserId = P.CreatedByUserId
INNER JOIN users_db.Profile as DeletedUser
	ON DeletedUser.UserId = DM.DeletedByUserID
WHERE
	P.Id IN (SELECT PID.Value FROM TempPageIds as PID)
--CUSTOM_ORDER_BEGIN::
--CONFIG::
/*
Page=P.[Name]
*/
--::CONFIG
ORDER BY
	P.[Name]
--::CUSTOM_ORDER_BEGIN
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize
