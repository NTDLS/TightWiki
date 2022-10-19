IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[GetCountOfPageAttachmentsById]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[GetCountOfPageAttachmentsById] AS'
END
GO



ALTER PROCEDURE [dbo].[GetCountOfPageAttachmentsById]
(
	@PageId int
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	SELECT
		Count(0) as Attachments
	FROM
		PageFile
	WHERE
		PageId = @PageId
		

END--PROCEDURE