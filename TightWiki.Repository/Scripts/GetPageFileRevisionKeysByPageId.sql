--Used against both the pages and the deleted pages databases.
SELECT
	PFR.PageFileId,
	PFR.Revision
FROM
	PageFileRevision as PFR
INNER JOIN PageFile as PF
	ON PF.Id = PFR.PageFileId
WHERE
	PF.PageId = @PageId
