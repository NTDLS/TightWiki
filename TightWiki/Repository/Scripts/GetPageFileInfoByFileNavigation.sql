SELECT
    PF.Id as PageFileId,
    PF.PageId,
    PF.Revision
FROM
    PageFile as PF
WHERE
	PF.PageId = @PageId
	AND PF.Navigation = @Navigation
