CREATE TABLE #tmp_c44a7c4971c24cc5adbe17c26b649b91 ([Id] [int],[Name] [nvarchar](128),[Description] [nvarchar](1000))
GO

INSERT INTO #tmp_c44a7c4971c24cc5adbe17c26b649b91 ([Name],[Description]) VALUES
('Basic',null),
('Search',null)
GO
ALTER TABLE [dbo].[ConfigurationGroup] NOCHECK CONSTRAINT ALL
GO
INSERT INTO [dbo].[ConfigurationGroup] (
	[Name],[Description])
SELECT
	[Name],[Description]
FROM #tmp_c44a7c4971c24cc5adbe17c26b649b91 as S
ALTER TABLE [dbo].[ConfigurationGroup] CHECK CONSTRAINT ALL
GO
DROP TABLE #tmp_c44a7c4971c24cc5adbe17c26b649b91
GO
