CREATE TABLE #tmp_f30a2db67263405a952d8c46ca096c5e ([Id] [int],[UserId] [int],[RoleId] [int])
GO

INSERT INTO #tmp_f30a2db67263405a952d8c46ca096c5e ([UserId],[RoleId]) VALUES
(1,1),
(2,3),
(2,5),
(7,1),
(15,3),
(16,3)
GO
ALTER TABLE [dbo].[UserRole] NOCHECK CONSTRAINT ALL
GO
INSERT INTO [dbo].[UserRole] (
	[UserId],[RoleId])
SELECT
	[UserId],[RoleId]
FROM #tmp_f30a2db67263405a952d8c46ca096c5e as S
WHERE NOT EXISTS (
	SELECT TOP 1 1 FROM  [dbo].[UserRole] as T
	WHERE 
)
ALTER TABLE [dbo].[UserRole] CHECK CONSTRAINT ALL
GO
DROP TABLE #tmp_f30a2db67263405a952d8c46ca096c5e
GO
