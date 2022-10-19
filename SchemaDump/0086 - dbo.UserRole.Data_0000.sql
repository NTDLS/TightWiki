CREATE TABLE #tmp_4372ea0d123a43d096cb6a296615bc7b ([Id] [int],[UserId] [int],[RoleId] [int])
GO

INSERT INTO #tmp_4372ea0d123a43d096cb6a296615bc7b ([UserId],[RoleId]) VALUES
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
FROM #tmp_4372ea0d123a43d096cb6a296615bc7b as S
WHERE NOT EXISTS (
	SELECT TOP 1 1 FROM  [dbo].[UserRole] as T
	WHERE 
)
ALTER TABLE [dbo].[UserRole] CHECK CONSTRAINT ALL
GO
DROP TABLE #tmp_4372ea0d123a43d096cb6a296615bc7b
GO
