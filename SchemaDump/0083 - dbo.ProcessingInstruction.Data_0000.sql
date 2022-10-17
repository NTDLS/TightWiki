CREATE TABLE #tmp_75b9438409fc45a4bb0e13a181bfcd58 ([PageId] [int],[Instruction] [nvarchar](128))
GO

INSERT INTO #tmp_75b9438409fc45a4bb0e13a181bfcd58 ([PageId],[Instruction]) VALUES
(1,'Protect')
GO
ALTER TABLE [dbo].[ProcessingInstruction] NOCHECK CONSTRAINT ALL
GO
UPDATE T SET
FROM [dbo].[ProcessingInstruction] as T
INNER JOIN #tmp_75b9438409fc45a4bb0e13a181bfcd58 as S
ON T.[PageId] = S.[PageId] AND T.[Instruction] = S.[Instruction]
GO
INSERT INTO [dbo].[ProcessingInstruction] (
	[PageId],[Instruction])
SELECT
	[PageId],[Instruction]
FROM #tmp_75b9438409fc45a4bb0e13a181bfcd58 as S
WHERE NOT EXISTS (
	SELECT TOP 1 1 FROM  [dbo].[ProcessingInstruction] as T
	WHERE T.[PageId] = S.[PageId] AND T.[Instruction] = S.[Instruction]
)
ALTER TABLE [dbo].[ProcessingInstruction] CHECK CONSTRAINT ALL
GO
DROP TABLE #tmp_75b9438409fc45a4bb0e13a181bfcd58
GO
