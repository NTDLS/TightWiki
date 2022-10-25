IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[UpdatePageProcessingInstructions]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[UpdatePageProcessingInstructions] AS'
END
GO

ALTER PROCEDURE [dbo].[UpdatePageProcessingInstructions]
(
	@PageId Int,
	@Instructions nVarChar(MAX)
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	BEGIN TRANSACTION

	DELETE FROM
		PageProcessingInstruction
	WHERE
		PageId = @PageId

	INSERT INTO PageProcessingInstruction
	(
		PageId,
		Instruction
	)
	SELECT
		@PageId,
		SP.[value]
	FROM
		STRING_SPLIT(@Instructions ,',') as SP
	WHERE
		IsNull(SP.[value], '') <> ''

	COMMIT TRANSACTION

END--PROCEDURE