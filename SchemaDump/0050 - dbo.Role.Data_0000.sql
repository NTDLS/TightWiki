CREATE TABLE #tmp_11f035d739754497805480e590914cc3 ([Id] [int],[Name] [nvarchar](128),[Description] [nvarchar](1000))
GO

INSERT INTO #tmp_11f035d739754497805480e590914cc3 ([Name],[Description]) VALUES
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
FROM #tmp_11f035d739754497805480e590914cc3 as S
ALTER TABLE [dbo].[Role] CHECK CONSTRAINT ALL
GO
DROP TABLE #tmp_11f035d739754497805480e590914cc3
GO
