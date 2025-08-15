SELECT
	P.Id as PageId,
	PR.[Name],
	PR.[Description],
	PR.Revision,
	P.Revision as HighestRevision,
	PR.ChangeSummary,
	P.Navigation,
	P.CreatedByUserId,
	Createduser.AccountName as CreatedByUserName,
	P.CreatedDate,
	PR.ModifiedByUserId,
	ModifiedUser.AccountName as ModifiedByUserName,
	PR.ModifiedDate,
	(SELECT COUNT(0) FROM PageRevision AS iPR WHERE iPR.PageId = P.Id AND iPR.Revision > PR.Revision) as HigherRevisionCount,
	@PageSize as PaginationPageSize,
	(
		SELECT
			CAST((Count(0) + (@PageSize - 1.0)) / @PageSize AS INTEGER)
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
LEFT OUTER JOIN users_db.Profile as ModifiedUser
	ON ModifiedUser.UserId = PR.ModifiedByUserId
LEFT OUTER JOIN users_db.Profile as Createduser
	ON Createduser.UserId = P.CreatedByUserId
WHERE
	P.Navigation = @Navigation
--CUSTOM_ORDER_BEGIN::
--CONFIG::
/*
Revision=PR.Revision
ModifiedBy=ModifiedUser.AccountName
ModifiedDate=PR.ModifiedDate
Page=PR.[Name]
*/
--::CONFIG
ORDER BY
	PR.Revision DESC
--::CUSTOM_ORDER_BEGIN
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize
