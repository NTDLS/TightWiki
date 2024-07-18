SELECT
	PageId,
	Instruction
FROM
	PageProcessingInstruction
WHERE
	PageId = @PageId