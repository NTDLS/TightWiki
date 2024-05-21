INSERT INTO PageRevisionAttachment
(
	PageId,
	PageFileId,
    FileRevision,
    PageRevision
)
SELECT
	PRA.PageId,
    PRA.PageFileId,
    PRA.FileRevision,
    PRA.PageRevision + 1
FROM
	PageRevisionAttachment as PRA
INNER JOIN PageFile as PF
	ON PF.Id = PRA.PageFileId
	AND PF.Revision = PRA.FileRevision
WHERE
	PRA.PageId = @PageId
	AND PRA.PageRevision = @PageRevision - 1
