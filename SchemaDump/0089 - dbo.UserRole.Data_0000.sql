CREATE TABLE #tmp_038e60ed756f4ff997c94561391bd5fb ([Id] [int],[UserId] [int],[RoleId] [int])
GO

INSERT INTO #tmp_038e60ed756f4ff997c94561391bd5fb ([UserId],[RoleId]) VALUES
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
FROM #tmp_038e60ed756f4ff997c94561391bd5fb as S
WHERE NOT EXISTS (
	SELECT TOP 1 1 FROM  [dbo].[UserRole] as T
	WHERE 
)
ALTER TABLE [dbo].[UserRole] CHECK CONSTRAINT ALL
GO
DROP TABLE #tmp_038e60ed756f4ff997c94561391bd5fb
GO
