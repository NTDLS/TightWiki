BEGIN TRANSACTION;

DELETE FROM
	PageProcessingInstruction
WHERE
	PageId = @PageId;

INSERT INTO PageProcessingInstruction
(
	PageId,
	Instruction
)
SELECT
	@PageId,
	TI.[value]
FROM
	TempInstructions as TI
WHERE
	Coalesce(TI.[value], '') <> '';

COMMIT TRANSACTION;
