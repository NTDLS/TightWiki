CREATE TABLE #tmp_d6c6c361600342b5ab171e05c78acced ([Id] [int],[Name] [nvarchar](128),[Description] [nvarchar](1000))
GO

INSERT INTO #tmp_d6c6c361600342b5ab171e05c78acced ([Name],[Description]) VALUES
('Basic',null)
GO
ALTER TABLE [dbo].[ConfigurationGroup] NOCHECK CONSTRAINT ALL
GO
INSERT INTO [dbo].[ConfigurationGroup] (
	[Name],[Description])
SELECT
	[Name],[Description]
FROM #tmp_d6c6c361600342b5ab171e05c78acced as S
ALTER TABLE [dbo].[ConfigurationGroup] CHECK CONSTRAINT ALL
GO
DROP TABLE #tmp_d6c6c361600342b5ab171e05c78acced
GO
