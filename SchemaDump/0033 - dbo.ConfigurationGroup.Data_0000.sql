CREATE TABLE #tmp_a16c3fd3f40643ebaffe63653eb85a1d ([Id] [int],[Name] [nvarchar](128),[Description] [nvarchar](1000))
GO

INSERT INTO #tmp_a16c3fd3f40643ebaffe63653eb85a1d ([Name],[Description]) VALUES
('Basic',null)
GO
ALTER TABLE [dbo].[ConfigurationGroup] NOCHECK CONSTRAINT ALL
GO
INSERT INTO [dbo].[ConfigurationGroup] (
	[Name],[Description])
SELECT
	[Name],[Description]
FROM #tmp_a16c3fd3f40643ebaffe63653eb85a1d as S
ALTER TABLE [dbo].[ConfigurationGroup] CHECK CONSTRAINT ALL
GO
DROP TABLE #tmp_a16c3fd3f40643ebaffe63653eb85a1d
GO
