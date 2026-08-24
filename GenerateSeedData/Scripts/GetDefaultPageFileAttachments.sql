SELECT
	P.Name as PageName,
	P.Namespace,
	PF.Name as FileName,
	PF.Navigation as FileNavigation,
	PFR.ContentType,
	PFR.Data
FROM
	PageFile as PF
INNER JOIN Page as P
	ON P.Id = PF.PageId
INNER JOIN PageFileRevision as PFR
	ON PFR.PageFileId = PF.Id
	AND PFR.Revision = PF.Revision;
