IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[GetPageProcessingInstructionsByPageId]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[GetPageProcessingInstructionsByPageId] AS'
END
GO

ALTER PROCEDURE [dbo].[GetPageProcessingInstructionsByPageId]
(
	@PageId Int
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	SELECT
		PageId,
		Instruction
	FROM
		PageProcessingInstruction
	WHERE
		PageId = @PageId

END--PROCEDURE