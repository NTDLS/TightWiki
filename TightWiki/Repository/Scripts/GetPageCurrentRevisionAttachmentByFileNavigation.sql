SELECT
    PF.PageId,
    PFA.PageFileId,
    PFR.Revision,
    PFR.ContentType,
    PFR.Size,
    PFR.DataHash
FROM
    PageFile as PF
INNER JOIN Page as P
	ON P.Id = PF.PageId
INNER JOIN PageRevisionAttachment as PFA
	ON PFA.PageFileId = PF.Id
	AND PFA.PageRevision = P.Revision
INNER JOIN PageFileRevision as PFR
	ON PFR.PageFileId = PF.Id
	AND PFR.Revision = P.Revision
WHERE
	PF.PageId = @PageId
	AND PF.Navigation = @Navigation
