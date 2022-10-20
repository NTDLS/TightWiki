IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[GetPageTagsById]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[GetPageTagsById] AS'
END
GO




ALTER PROCEDURE [dbo].[GetPageTagsById]
(
	@PageId int
)AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	SELECT
		PT.Tag
	FROM
		[PageTag] as PT
	WHERE
		PT.PageId = @PageId

END--PROCEDURE