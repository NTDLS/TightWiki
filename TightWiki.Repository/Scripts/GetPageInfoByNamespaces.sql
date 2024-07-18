SELECT DISTINCT
	P.Id,
	P.[Name],
	P.[Description],
	P.Navigation,
	P.CreatedByUserId,
	P.CreatedDate,
	P.ModifiedByUserId,
	P.ModifiedDate
FROM
	[Page] as P
INNER JOIN TempNamespaces as TN
	ON TN.[value] = P.[Namespace]
