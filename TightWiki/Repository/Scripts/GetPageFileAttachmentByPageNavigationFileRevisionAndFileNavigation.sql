
SELECT
	PFR.Revision,
	PF.[Id] as [Id],
	PF.[PageId] as [PageId],
	PF.[Name] as [Name],
	PFR.[ContentType] as [ContentType],
	PFR.[Size] as [Size],
	PF.[CreatedDate] as [CreatedDate],
	PFR.[Data] as [Data]
FROM
	PageFile as PF
INNER JOIN [Page] as P
	ON P.Id = PF.PageId
INNER JOIN PageFileRevision as PFR
	ON PFR.PageFileId = PF.Id
WHERE
	P.Navigation = @PageNavigation
	AND PF.Navigation = @FileNavigation
	AND PFR.Revision = Coalesce(@FileRevision, PF.Revision)
