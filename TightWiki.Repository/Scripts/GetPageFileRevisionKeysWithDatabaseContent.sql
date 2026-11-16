--Revisions whose content is still in the database instead of the attachment storage.
--Used against both the pages and the deleted pages databases.
SELECT
	PageFileId,
	Revision
FROM
	PageFileRevision
WHERE
	length(Data) > 0
ORDER BY
	PageFileId,
	Revision
