SELECT
	COUNT(0) as TotalCount,
	COALESCE(SUM(PFR.Size), 0) as TotalSize,
	COALESCE(SUM(CASE WHEN PRA.PageFileId IS NULL THEN 1 ELSE 0 END), 0) as OrphanedCount,
	COALESCE(SUM(CASE WHEN PRA.PageFileId IS NULL THEN PFR.Size ELSE 0 END), 0) as OrphanedSize
FROM
	PageFileRevision as PFR
INNER JOIN PageFile as PF
	ON PF.Id = PFR.PageFileId
INNER JOIN Page as P
	ON P.Id = PF.PageId
LEFT OUTER JOIN (
	SELECT DISTINCT PageFileId, FileRevision FROM PageRevisionAttachment
) as PRA
	ON PRA.PageFileId = PFR.PageFileId
	AND PRA.FileRevision = PFR.Revision
