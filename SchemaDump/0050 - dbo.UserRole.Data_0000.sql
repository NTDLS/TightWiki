CREATE TABLE #tmp_cb3816c77c8b4e79ae90e6a17c922bb5 ([Id] [int],[UserId] [int],[RoleId] [int])
GO

INSERT INTO #tmp_cb3816c77c8b4e79ae90e6a17c922bb5 ([UserId],[RoleId]) VALUES
(1,1),
(2,1)
GO
ALTER TABLE [dbo].[UserRole] NOCHECK CONSTRAINT ALL
GO
INSERT INTO [dbo].[UserRole] (
	[UserId],[RoleId])
SELECT
	[UserId],[RoleId]
FROM #tmp_cb3816c77c8b4e79ae90e6a17c922bb5 as S
WHERE NOT EXISTS (
	SELECT TOP 1 1 FROM  [dbo].[UserRole] as T
	WHERE 
)
ALTER TABLE [dbo].[UserRole] CHECK CONSTRAINT ALL
GO
DROP TABLE #tmp_cb3816c77c8b4e79ae90e6a17c922bb5
GO
