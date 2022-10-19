IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[DeletePageFileByPageNavigationAndFileName]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[DeletePageFileByPageNavigationAndFileName] AS'
END
GO



ALTER PROCEDURE [dbo].[DeletePageFileByPageNavigationAndFileName]
(
	@PageNavigation nVarChar(128),
    @FileNavigation nVarChar(250)
) AS
BEGIN--PROCEDURE

	DELETE
		PRA
	FROM
		[PageFile] as PF
	INNER JOIN [Page] as P
		ON P.Id = PF.PageId
	INNER JOIN [PageRevision] as PR
		ON PR.PageId = P.Id
	INNER JOIN PageRevisionAttachment as PRA
		ON PRA.PageId = P.Id
		AND PRA.PageFileId = PF.Id
		AND PRA.PageRevision = PR.Revision
	INNER JOIN PageFileRevision as PFR
		ON PFR.PageFileId = PF.Id
		AND PFR.Revision = PRA.FileRevision
	WHERE
		P.Navigation = @PageNavigation
		AND PF.Navigation = @FileNavigation
		
END--PROCEDURE