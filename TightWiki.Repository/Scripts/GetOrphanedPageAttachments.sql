SELECT
	PFR.PageFileId,
	P.Name as PageName,
	P.Namespace,
	P.Navigation as PageNavigation,
	PF.Name as FileName,
	PF.Navigation as FileNavigation,
	PFR.Size,
	PFR.Revision as FileRevision,
	@PageSize as PaginationPageSize,
	(
		SELECT
			(Round(Count(0) / (@PageSize + 0.0) + 0.999))
		FROM
			PageFileRevision as PFR
		INNER JOIN PageFile as PF
			ON PF.Id = PFR.PageFileId
		INNER JOIN Page as P
			ON P.Id = PF.PageId
		LEFT OUTER JOIN PageRevisionAttachment as PRA
			ON PRA.PageFileId = PFR.PageFileId
			AND PRA.FileRevision = PFR.Revision
		WHERE
			PRA.PageFileId IS NULL
	) as PaginationPageCount
FROM
	PageFileRevision as PFR
INNER JOIN PageFile as PF
	ON PF.Id = PFR.PageFileId
INNER JOIN Page as P
	ON P.Id = PF.PageId
LEFT OUTER JOIN PageRevisionAttachment as PRA
	ON PRA.PageFileId = PFR.PageFileId
	AND PRA.FileRevision = PFR.Revision
WHERE
	PRA.PageFileId IS NULL
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize