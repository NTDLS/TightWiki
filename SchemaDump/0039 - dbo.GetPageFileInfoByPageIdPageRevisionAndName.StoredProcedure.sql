IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[GetPageFileInfoByPageIdPageRevisionAndName]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[GetPageFileInfoByPageIdPageRevisionAndName] AS'
END
GO


ALTER PROCEDURE [dbo].[GetPageFileInfoByPageIdPageRevisionAndName]
(
	@PageId Int,
	@FileName nVarChar(500),
	@PageRevision int = NULL
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	SELECT
		PF.[Id] as [Id],
		PF.[PageId] as [PageId],
		PF.[Name] as [Name],
		PFR.[ContentType] as [ContentType],
		PFR.[Size] as [Size],
		PF.[CreatedDate] as [CreatedDate]
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
		P.Id = @PageId
		AND PF.[Name] = @FileName
		AND PR.Revision = IsNull(@PageRevision, P.Revision)

END--PROCEDURE