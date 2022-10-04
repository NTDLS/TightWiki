CREATE TABLE #tmp_8a872b5e29304983b36496bf5dc1e666 ([Id] [int],[UserId] [int],[RoleId] [int])
GO

INSERT INTO #tmp_8a872b5e29304983b36496bf5dc1e666 ([UserId],[RoleId]) VALUES
(1,1),
(2,3),
(2,5),
(7,1)
GO
ALTER TABLE [dbo].[UserRole] NOCHECK CONSTRAINT ALL
GO
INSERT INTO [dbo].[UserRole] (
	[UserId],[RoleId])
SELECT
	[UserId],[RoleId]
FROM #tmp_8a872b5e29304983b36496bf5dc1e666 as S
WHERE NOT EXISTS (
	SELECT TOP 1 1 FROM  [dbo].[UserRole] as T
	WHERE 
)
ALTER TABLE [dbo].[UserRole] CHECK CONSTRAINT ALL
GO
DROP TABLE #tmp_8a872b5e29304983b36496bf5dc1e666
GO
