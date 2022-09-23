CREATE TABLE #tmp_2608bbf7c0114bb7a52aca9aa9a280ab ([Id] [int],[ConfigurationGroupId] [int],[Name] [nvarchar](128),[Value] [nvarchar](255),[Description] [nvarchar](1000))
GO

INSERT INTO #tmp_2608bbf7c0114bb7a52aca9aa9a280ab ([ConfigurationGroupId],[Name],[Value],[Description]) VALUES
(1,'Copyright','Copyright &copy; NetworkDLS 2022',null),
(1,'FooterBlurb','ASAP!Wiki! is a free and opensource .NET MVC Wiki built on top of SQL Server and various other Microsoft technologies. Feel free to tweak, contort and otherwise use as you desire. Enjoy!',null),
(1,'Name','ASAP! WIKI!',null),
(1,'New Page Template','##SetTags(draft)
{{{(Panel, Table of Contents) ##toc }}}

==Topic #1
*Lets get started!

',null)
GO
ALTER TABLE [dbo].[ConfigurationEntry] NOCHECK CONSTRAINT ALL
GO
UPDATE T SET 
T.[Value] = S.[Value],
T.[Description] = S.[Description]
FROM [dbo].[ConfigurationEntry] as T
INNER JOIN #tmp_2608bbf7c0114bb7a52aca9aa9a280ab as S
ON T.[ConfigurationGroupId] = S.[ConfigurationGroupId] AND T.[Name] = S.[Name]
GO
INSERT INTO [dbo].[ConfigurationEntry] (
	[ConfigurationGroupId],[Name],[Value],[Description])
SELECT
	[ConfigurationGroupId],[Name],[Value],[Description]
FROM #tmp_2608bbf7c0114bb7a52aca9aa9a280ab as S
WHERE NOT EXISTS (
	SELECT TOP 1 1 FROM  [dbo].[ConfigurationEntry] as T
	WHERE T.[ConfigurationGroupId] = S.[ConfigurationGroupId] AND T.[Name] = S.[Name]
)
ALTER TABLE [dbo].[ConfigurationEntry] CHECK CONSTRAINT ALL
GO
DROP TABLE #tmp_2608bbf7c0114bb7a52aca9aa9a280ab
GO
