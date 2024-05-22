SELECT
	PF.[Id] as [Id],
	PF.[PageId] as [PageId],
	PF.[Name] as [Name],
	PFR.[ContentType] as [ContentType],
	PFR.[Size] as [Size],
	PFR.[Data],
	PF.[CreatedDate] as [CreatedDate],
	PFR.[Data] as [Data]
FROM
	[PageFile] as PF
INNER JOIN [Page] as P
	ON P.Id = PF.PageId
INNER JOIN PageRevisionAttachment as PRA
	ON PRA.PageId = P.Id
	AND PRA.PageFileId = PF.Id
INNER JOIN (
	SELECT
		PF.Id as PageFileId,
		PR.Revision as PageRevision,
		MAX(PRA.FileRevision) as LatestAttachedFileRevision
	FROM
		Page as P
	INNER JOIN PageFile as PF
		ON PF.PageId = P.Id
	INNER JOIN [PageRevision] as PR
		ON PR.PageId = P.Id	
	INNER JOIN PageRevisionAttachment as PRA
		ON PRA.PageId = P.Id
		AND PRA.PageRevision = PR.Revision
	WHERE
		P.Navigation = @PageNavigation
		AND PF.Navigation = @FileNavigation
		AND PR.Revision = Coalesce(@PageRevision, P.Revision)
	GROUP BY
		PF.Id
	) as Latest
	ON Latest.PageFileId = PF.Id
	AND Latest.LatestAttachedFileRevision = PRA.FileRevision
INNER JOIN PageFileRevision as PFR
	ON PFR.PageFileId = PF.Id
	AND PFR.Revision = Latest.LatestAttachedFileRevision	
WHERE
	P.Navigation = @PageNavigation
	AND PF.Navigation = @FileNavigation
	AND PRA.PageRevision = Latest.PageRevision

	
	
