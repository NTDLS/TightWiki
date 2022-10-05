CREATE TABLE #tmp_5e596b632ec04e4c80523a4dda33cb51 ([Id] [int],[UserId] [int],[RoleId] [int])
GO

INSERT INTO #tmp_5e596b632ec04e4c80523a4dda33cb51 ([UserId],[RoleId]) VALUES
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
FROM #tmp_5e596b632ec04e4c80523a4dda33cb51 as S
WHERE NOT EXISTS (
	SELECT TOP 1 1 FROM  [dbo].[UserRole] as T
	WHERE 
)
ALTER TABLE [dbo].[UserRole] CHECK CONSTRAINT ALL
GO
DROP TABLE #tmp_5e596b632ec04e4c80523a4dda33cb51
GO
