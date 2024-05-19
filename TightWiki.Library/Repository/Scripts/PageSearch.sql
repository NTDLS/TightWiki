SELECT
	P.Id,
	ST.[Score],
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
	Coalesce(ModifiedUser.AccountName, '') as ModifiedByUserName
FROM
	[Page] as P
LEFT OUTER JOIN Profile as ModifiedUser
	ON ModifiedUser.UserId = P.ModifiedByUserId
LEFT OUTER JOIN Profile as Createduser
	ON Createduser.UserId = P.CreatedByUserId
INNER JOIN TempSearchTerms as ST
	ON ST.PageId = P.Id
ORDER BY
	ST.[Score] DESC,
	P.[Name],
	P.Id
