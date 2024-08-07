SELECT
	PFR.Revision,
	PF.[Id] as [Id],
	PF.[PageId] as [PageId],
	PF.[Name] as [Name],
	PFR.[ContentType] as [ContentType],
	PFR.[Size] as [Size],
	PFR.[CreatedDate] as [CreatedDate],
	PFR.Revision as FileRevision,
	UP.UserID as CreatedByUserId,
	UP.AccountName as CreatedByUserName,
	UP.Navigation as CreatedByNavigation,
	@PageSize as PaginationPageSize,
	(
		SELECT
			CAST((Count(0) + (@PageSize - 1.0)) / @PageSize AS INTEGER)
		FROM
			PageFile as PF
		INNER JOIN [Page] as P
			ON P.Id = PF.PageId
		INNER JOIN PageFileRevision as PFR
			ON PFR.PageFileId = PF.Id
		LEFT OUTER JOIN users_db.Profile UP
			ON UP.UserId = PFR.CreatedByUserId
		WHERE
			P.Navigation = @PageNavigation
			AND PF.Navigation = @FileNavigation
	) as PaginationPageCount
FROM
	PageFile as PF
INNER JOIN [Page] as P
	ON P.Id = PF.PageId
INNER JOIN PageFileRevision as PFR
	ON PFR.PageFileId = PF.Id
LEFT OUTER JOIN users_db.Profile UP
	ON UP.UserId = PFR.CreatedByUserId
WHERE
	P.Navigation = @PageNavigation
	AND PF.Navigation = @FileNavigation
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize;
