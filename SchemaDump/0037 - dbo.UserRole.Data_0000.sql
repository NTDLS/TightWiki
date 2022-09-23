CREATE TABLE #tmp_b4767d8f17044c95831434eacf7c3549 ([Id] [int],[UserId] [int],[RoleId] [int])
GO

INSERT INTO #tmp_b4767d8f17044c95831434eacf7c3549 ([UserId],[RoleId]) VALUES
(1,1),
(2,1)
GO
ALTER TABLE [dbo].[UserRole] NOCHECK CONSTRAINT ALL
GO
INSERT INTO [dbo].[UserRole] (
	[UserId],[RoleId])
SELECT
	[UserId],[RoleId]
FROM #tmp_b4767d8f17044c95831434eacf7c3549 as S
WHERE NOT EXISTS (
	SELECT TOP 1 1 FROM  [dbo].[UserRole] as T
	WHERE 
)
ALTER TABLE [dbo].[UserRole] CHECK CONSTRAINT ALL
GO
DROP TABLE #tmp_b4767d8f17044c95831434eacf7c3549
GO
