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
	@PageSize as PaginationSize,
	(
		SELECT
			Round(Count(0) / (@PageSize + 0.0)  + 0.999)
		FROM
			PageFile as PF
		INNER JOIN [Page] as P
			ON P.Id = PF.PageId
		INNER JOIN PageFileRevision as PFR
			ON PFR.PageFileId = PF.Id
		INNER JOIN Profile UP
			ON UP.UserId = PFR.CreatedByUserId
		WHERE
			P.Navigation = @PageNavigation
			AND PF.Navigation = @FileNavigation
	) as PaginationCount
FROM
	PageFile as PF
INNER JOIN [Page] as P
	ON P.Id = PF.PageId
INNER JOIN PageFileRevision as PFR
	ON PFR.PageFileId = PF.Id
INNER JOIN Profile UP
	ON UP.UserId = PFR.CreatedByUserId
WHERE
	P.Navigation = @PageNavigation
	AND PF.Navigation = @FileNavigation
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize
