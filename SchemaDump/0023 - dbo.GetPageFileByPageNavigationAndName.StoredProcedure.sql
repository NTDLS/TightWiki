IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[GetPageFileByPageNavigationAndName]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[GetPageFileByPageNavigationAndName] AS'
END
GO


ALTER PROCEDURE [dbo].[GetPageFileByPageNavigationAndName]
(
	@PageNavigation nVarChar(128),
	@ImageName VarChar(255)
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	SELECT
		PF.[Id] as [Id],
		PF.[PageId] as [PageId],
		PF.[Name] as [Name],
		PF.[ContentType] as [ContentType],
		PF.[Size] as [Size],
		PF.[CreatedDate] as [CreatedDate],
		PF.[Data] as [Data]
	FROM
		[PageFile] as PF
	INNER JOIN [Page] as P
		ON P.Id = PF.PageId
	WHERE
		P.Navigation = @PageNavigation
		AND PF.[Name] = @ImageName

END--PROCEDURE