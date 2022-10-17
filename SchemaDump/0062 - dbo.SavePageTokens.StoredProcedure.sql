IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[SavePageTokens]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[SavePageTokens] AS'
END
GO

ALTER PROCEDURE [dbo].[SavePageTokens]
(
	@PageTokens dbo.PageTokenType READONLY
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	BEGIN TRANSACTION

	DELETE FROM
		[dbo].[PageToken]
	WHERE
		[PageId] IN (SELECT PageId FROM @PageTokens)

	INSERT INTO [dbo].[PageToken]
	(
		[PageId],
		[Token],
		[Weight]
	)
	SELECT
		[PageId],
		[Token],
		[Weight]
	FROM
		@PageTokens

	COMMIT TRANSACTION

END--PROCEDURE