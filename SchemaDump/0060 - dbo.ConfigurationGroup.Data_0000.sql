CREATE TABLE #tmp_125dad20889848feba964899c4c18cf1 ([Id] [int],[Name] [nvarchar](128),[Description] [nvarchar](1000))
GO

INSERT INTO #tmp_125dad20889848feba964899c4c18cf1 ([Name],[Description]) VALUES
('Basic','Basic wiki settings such as formatting.'),
('Search','Configuration related to searching and indexing.'),
('Functionality',null),
('Membership','Membership settings such as defaults for new members and permissions.'),
('Email','EMail and SMTP setting.')
GO
ALTER TABLE [dbo].[ConfigurationGroup] NOCHECK CONSTRAINT ALL
GO
INSERT INTO [dbo].[ConfigurationGroup] (
	[Name],[Description])
SELECT
	[Name],[Description]
FROM #tmp_125dad20889848feba964899c4c18cf1 as S
ALTER TABLE [dbo].[ConfigurationGroup] CHECK CONSTRAINT ALL
GO
DROP TABLE #tmp_125dad20889848feba964899c4c18cf1
GO
