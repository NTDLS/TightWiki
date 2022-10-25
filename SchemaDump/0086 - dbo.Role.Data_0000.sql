CREATE TABLE #tmp_b2fd1e24c07b46f3acb11853998c8320 ([Id] [int],[Name] [nvarchar](128),[Description] [nvarchar](1000))
GO

INSERT INTO #tmp_b2fd1e24c07b46f3acb11853998c8320 ([Name],[Description]) VALUES
('Administrator','Administrators can do anything. Add, edit, delete, etc.'),
('Member','Read-only user with a profile.'),
('Contributor','Contributor can add and edit pages.'),
('Moderator','Moderators can add, edit and delete pages.')
GO
ALTER TABLE [dbo].[Role] NOCHECK CONSTRAINT ALL
GO
INSERT INTO [dbo].[Role] (
	[Name],[Description])
SELECT
	[Name],[Description]
FROM #tmp_b2fd1e24c07b46f3acb11853998c8320 as S
ALTER TABLE [dbo].[Role] CHECK CONSTRAINT ALL
GO
DROP TABLE #tmp_b2fd1e24c07b46f3acb11853998c8320
GO
