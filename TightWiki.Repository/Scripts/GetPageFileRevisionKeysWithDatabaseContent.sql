--Revisions whose content is still in the database instead of the attachment storage.
--Used against both the pages and the deleted pages databases.
SELECT
	PFR.PageFileId,
	PFR.Revision,
	PF.Name
FROM
	PageFileRevision as PFR
INNER JOIN PageFile as PF
	ON PF.Id = PFR.PageFileId
WHERE
	length(PFR.Data) > 0
ORDER BY
	PFR.PageFileId,
	PFR.Revision
