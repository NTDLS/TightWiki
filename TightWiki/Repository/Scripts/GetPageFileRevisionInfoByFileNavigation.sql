SELECT
    PF.PageId,
    PFR.PageFileId,
    PFR.Revision,
    PFR.ContentType,
    PFR.Size,
    PFR.DataHash
FROM
	PageFileRevision as PFR
INNER JOIN PageFile as PF
    ON PF.Id = PFR.PageFileId
	AND PFR.Revision = PF.Revision
WHERE
	PF.PageId = @PageId
	AND PF.Navigation = @Navigation
	