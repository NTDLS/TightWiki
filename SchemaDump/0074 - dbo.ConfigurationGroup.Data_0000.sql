CREATE TABLE #tmp_0853235b4c034edca78526d9174ce00e ([Id] [int],[Name] [nvarchar](128),[Description] [nvarchar](1000))
GO

INSERT INTO #tmp_0853235b4c034edca78526d9174ce00e ([Name],[Description]) VALUES
('Basic','Basic wiki settings such as formatting.'),
('Search','Configuration related to searching and indexing.'),
('Functionality',null),
('Membership','Membership settings such as defaults for new members and permissions.'),
('Email','EMail and SMTP setting.'),
('HTML Layout','HTML layout')
GO
ALTER TABLE [dbo].[ConfigurationGroup] NOCHECK CONSTRAINT ALL
GO
INSERT INTO [dbo].[ConfigurationGroup] (
	[Name],[Description])
SELECT
	[Name],[Description]
FROM #tmp_0853235b4c034edca78526d9174ce00e as S
ALTER TABLE [dbo].[ConfigurationGroup] CHECK CONSTRAINT ALL
GO
DROP TABLE #tmp_0853235b4c034edca78526d9174ce00e
GO
