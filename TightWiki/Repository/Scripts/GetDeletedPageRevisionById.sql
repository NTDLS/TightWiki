SELECT
	PR.PageId as Id,
	PR.Name,
	PR.[Description],
	PR.Revision as Revision,
	PR.Body,
	DM.DeletedDate,
	DeletedUser.AccountName as DeletedByUserName
FROM
	[PageRevision] as PR
INNER JOIN DeletionMeta as DM
	ON DM.PageId = PR.PageId
	AND DM.Revision = PR.Revision
INNER JOIN users_db.Profile as DeletedUser
	ON DeletedUser.UserId = DM.DeletedByUserID
WHERE
	PR.PageId = @PageId
	AND PR.Revision = @Revision
