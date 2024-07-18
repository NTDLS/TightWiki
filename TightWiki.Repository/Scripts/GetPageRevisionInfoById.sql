SELECT
	P.Id,
	P.[Name],
	P.[Description],
	PR.Revision,
	P.Navigation,
	P.CreatedByUserId,
	--Createduser.AccountName as CreatedByUserName,
	P.CreatedDate,
	PR.ModifiedByUserId,
	--ModifiedUser.AccountName as ModifiedByUserName,
	PR.ModifiedDate
FROM
	[Page] as P
INNER JOIN [PageRevision] as PR
	ON PR.PageId = P.Id
--INNER JOIN [User] as ModifiedUser
--	ON ModifiedUser.Id = PR.ModifiedByUserId
--INNER JOIN [User] as Createduser
--	ON Createduser.Id = P.CreatedByUserId
WHERE
	P.Id = @PageId
	AND PR.Revision = COALESCE(@Revision, P.Revision)