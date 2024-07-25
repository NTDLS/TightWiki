SELECT
	P.Id,
	P.[Name],
	PR.[Description],
	PR.Body,
	PR.Revision,
	P.Revision as MostCurrentRevision,
	P.Navigation,
	P.CreatedByUserId,
	P.CreatedDate,
	PR.ModifiedByUserId,
	PR.ModifiedDate,
	DM.DeletedDate,
	Createduser.AccountName as CreatedByUserName,
	ModifiedUser.AccountName as ModifiedByUserName,
	DeletedUser.AccountName as DeletedByUserName
FROM
	[Page] as P
INNER JOIN [PageRevision] as PR
	ON PR.PageId = P.Id
	AND PR.Revision = P.Revision
INNER JOIN DeletionMeta as DM
	ON DM.PageId = P.Id
LEFT OUTER JOIN users_db.Profile as ModifiedUser
	ON ModifiedUser.UserId = P.ModifiedByUserId
LEFT OUTER JOIN users_db.Profile as Createduser
	ON Createduser.UserId = P.CreatedByUserId
LEFT OUTER JOIN users_db.Profile as DeletedUser
	ON DeletedUser.UserId = DM.DeletedByUserID
WHERE
	P.Id = @PageId
