CREATE TABLE #tmp_e350dcf3256e480c80cccf81a9e277eb ([Id] [int],[UserId] [int],[RoleId] [int])
GO

INSERT INTO #tmp_e350dcf3256e480c80cccf81a9e277eb ([UserId],[RoleId]) VALUES
(1,1),
(2,1)
GO
ALTER TABLE [dbo].[UserRole] NOCHECK CONSTRAINT ALL
GO
INSERT INTO [dbo].[UserRole] (
	[UserId],[RoleId])
SELECT
	[UserId],[RoleId]
FROM #tmp_e350dcf3256e480c80cccf81a9e277eb as S
WHERE NOT EXISTS (
	SELECT TOP 1 1 FROM  [dbo].[UserRole] as T
	WHERE 
)
ALTER TABLE [dbo].[UserRole] CHECK CONSTRAINT ALL
GO
DROP TABLE #tmp_e350dcf3256e480c80cccf81a9e277eb
GO
