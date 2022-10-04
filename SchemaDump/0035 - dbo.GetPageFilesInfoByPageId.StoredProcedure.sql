IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[GetPageFilesInfoByPageId]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[GetPageFilesInfoByPageId] AS'
END
GO

ALTER PROCEDURE [dbo].[GetPageFilesInfoByPageId]
(
	@PageId Int
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

END--PROCEDURE