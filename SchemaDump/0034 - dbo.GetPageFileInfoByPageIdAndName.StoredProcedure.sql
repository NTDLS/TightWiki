IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[GetPageFileInfoByPageIdAndName]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[GetPageFileInfoByPageIdAndName] AS'
END
GO

ALTER PROCEDURE [dbo].[GetPageFileInfoByPageIdAndName]
(
	@PageId Int,
	@FileName nVarChar(500)
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	SELECT
		[Id],
		[PageId],
		[Name],
		[ContentType],
		[Size],
		[CreatedDate]
	FROM
		[PageFile]
	WHERE
		PageId = @PageId
		AND [Name] = @FileName

END--PROCEDURE