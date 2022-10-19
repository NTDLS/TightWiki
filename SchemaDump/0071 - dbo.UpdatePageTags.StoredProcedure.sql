IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[UpdatePageTags]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[UpdatePageTags] AS'
END
GO




ALTER PROCEDURE [dbo].[UpdatePageTags]
(
	@PageId Int,
	@Tags nVarChar(MAX)
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	BEGIN TRANSACTION

	DELETE FROM
		PageTag
	WHERE
		PageId = @PageId

	INSERT INTO PageTag
	(
		PageId,
		[Tag]
	)
	SELECT
		@PageId,
		SP.[value]
	FROM
		STRING_SPLIT(@Tags ,',') as SP
	WHERE
		IsNull(SP.[value], '') <> ''

	COMMIT TRANSACTION

END--PROCEDURE