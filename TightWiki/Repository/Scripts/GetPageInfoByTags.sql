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
	PageTag as PT
INNER JOIN TempTags AS T
	ON T.Value = PT.Tag
INNER JOIN [Page] as P
	ON P.Id = PT.PageId
