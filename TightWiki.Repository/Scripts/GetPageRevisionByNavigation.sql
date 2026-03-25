SELECT
	P.Id,
	P.[Name],
	PR.[Description],
	PR.Body,
	PR.Revision,
	PR.ChangeSummary,
	P.Revision as MostCurrentRevision,
	P.Navigation,
	P.CreatedByUserId,
	P.CreatedDate,
	PR.ModifiedByUserId,
	PR.ModifiedDate,
	MBU.AccountName as ModifiedByUserName,
	CBU.AccountName as CreatedByUserName,
	(SELECT COUNT(0) FROM PageRevision AS iPR
		WHERE iPR.PageId = P.Id AND iPR.Revision > PR.Revision) as HigherRevisionCount
FROM
	[Page] as P
INNER JOIN [PageRevision] as PR
	ON PR.PageId = P.Id
LEFT OUTER JOIN users_db.Profile as MBU
	ON MBU.UserId = P.ModifiedByUserId
LEFT OUTER JOIN users_db.Profile as CBU
	ON MBU.UserId = P.CreatedByUserId
WHERE
	P.Navigation = @Navigation
	AND PR.Revision = COALESCE(@Revision, P.Revision)
