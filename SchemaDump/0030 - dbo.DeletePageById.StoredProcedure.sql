IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[DeletePageById]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[DeletePageById] AS'
END
GO



ALTER PROCEDURE [dbo].[DeletePageById]
(
	@PageId int
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	DELETE FROM [PageRevision] WHERE PageId = @PageId
	DELETE FROM [PageTag] WHERE PageId = @PageId
	DELETE FROM [PageRevisionAttachment] WHERE PageId = @PageId
	DELETE FROM [PageFile] WHERE PageId = @PageId
	DELETE FROM [PageToken] WHERE PageId = @PageId
	DELETE FROM [ProcessingInstruction] WHERE PageId = @PageId
	DELETE FROM [Page] WHERE Id = @PageId

END--PROCEDURE