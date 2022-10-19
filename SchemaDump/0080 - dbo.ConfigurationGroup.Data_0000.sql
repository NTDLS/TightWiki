CREATE TABLE #tmp_e08b6d43d3a14f7bb68bd8a5298bd5ea ([Id] [int],[Name] [nvarchar](128),[Description] [nvarchar](1000))
GO

INSERT INTO #tmp_e08b6d43d3a14f7bb68bd8a5298bd5ea ([Name],[Description]) VALUES
('Basic','Basic wiki settings such as formatting.'),
('Search','Configuration related to searching and indexing.'),
('Functionality','General wiki functionality.'),
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
FROM #tmp_e08b6d43d3a14f7bb68bd8a5298bd5ea as S
ALTER TABLE [dbo].[ConfigurationGroup] CHECK CONSTRAINT ALL
GO
DROP TABLE #tmp_e08b6d43d3a14f7bb68bd8a5298bd5ea
GO
