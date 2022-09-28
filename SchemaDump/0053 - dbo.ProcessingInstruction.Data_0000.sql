CREATE TABLE #tmp_08c4263858ca4434a5fd0e60d5eb7bb3 ([PageId] [int],[Instruction] [nvarchar](128))
GO

INSERT INTO #tmp_08c4263858ca4434a5fd0e60d5eb7bb3 ([PageId],[Instruction]) VALUES
(36,'Draft'),
(55,'Draft'),
(56,'Draft'),
(59,'Draft'),
(63,'Draft'),
(64,'Draft'),
(65,'Draft'),
(66,'Draft'),
(85,'Draft'),
(90,'Draft'),
(93,'Draft'),
(94,'Draft'),
(95,'Draft'),
(97,'Draft'),
(110,'Draft'),
(113,'Draft'),
(117,'Draft'),
(120,'Draft'),
(122,'Draft'),
(167,'Draft'),
(168,'Draft'),
(172,'Draft'),
(181,'Draft'),
(185,'Draft'),
(200,'Draft'),
(202,'Draft'),
(204,'Draft'),
(212,'Draft'),
(214,'Draft'),
(230,'Draft'),
(232,'Draft'),
(233,'Draft'),
(234,'Draft'),
(238,'Draft'),
(239,'Draft'),
(242,'Draft'),
(243,'Draft'),
(248,'Draft'),
(254,'Draft')
GO
ALTER TABLE [dbo].[ProcessingInstruction] NOCHECK CONSTRAINT ALL
GO
UPDATE T SET
FROM [dbo].[ProcessingInstruction] as T
INNER JOIN #tmp_08c4263858ca4434a5fd0e60d5eb7bb3 as S
ON T.[PageId] = S.[PageId] AND T.[Instruction] = S.[Instruction]
GO
INSERT INTO [dbo].[ProcessingInstruction] (
	[PageId],[Instruction])
SELECT
	[PageId],[Instruction]
FROM #tmp_08c4263858ca4434a5fd0e60d5eb7bb3 as S
WHERE NOT EXISTS (
	SELECT TOP 1 1 FROM  [dbo].[ProcessingInstruction] as T
	WHERE T.[PageId] = S.[PageId] AND T.[Instruction] = S.[Instruction]
)
ALTER TABLE [dbo].[ProcessingInstruction] CHECK CONSTRAINT ALL
GO
DROP TABLE #tmp_08c4263858ca4434a5fd0e60d5eb7bb3
GO
