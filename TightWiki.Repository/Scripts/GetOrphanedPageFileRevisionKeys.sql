--The same rows that PurgeOrphanedPageAttachments.sql deletes.
SELECT
	PFR.PageFileId,
	PFR.Revision
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
