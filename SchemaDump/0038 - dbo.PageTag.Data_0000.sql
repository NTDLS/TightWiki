CREATE TABLE #tmp_a85dfe253b93442790a8553bae0a30fa ([PageId] [int],[Tag] [nvarchar](128))
GO

INSERT INTO #tmp_a85dfe253b93442790a8553bae0a30fa ([PageId],[Tag]) VALUES
(1,'Canned'),
(1,'Default'),
(1,'Example'),
(1,'Home Page'),
(1,'NetworkDLS'),
(1,'Official-Help'),
(1,'Test'),
(1,'The Home Page'),
(3,'Official-Help'),
(5,'notes'),
(6,'Official-Help'),
(8,'draft'),
(8,'Shoes'),
(22,'Canned'),
(22,'Default'),
(22,'Example'),
(22,'Home Page'),
(22,'NetworkDLS'),
(22,'Test'),
(22,'The Home Page'),
(23,'Official-Help'),
(24,'Official-Help'),
(25,'Official-Help'),
(26,'Official-Help'),
(27,'Official-Help')
GO
ALTER TABLE [dbo].[PageTag] NOCHECK CONSTRAINT ALL
GO
UPDATE T SET
FROM [dbo].[PageTag] as T
INNER JOIN #tmp_a85dfe253b93442790a8553bae0a30fa as S
ON T.[PageId] = S.[PageId] AND T.[Tag] = S.[Tag]
GO
INSERT INTO [dbo].[PageTag] (
	[PageId],[Tag])
SELECT
	[PageId],[Tag]
FROM #tmp_a85dfe253b93442790a8553bae0a30fa as S
WHERE NOT EXISTS (
	SELECT TOP 1 1 FROM  [dbo].[PageTag] as T
	WHERE T.[PageId] = S.[PageId] AND T.[Tag] = S.[Tag]
)
ALTER TABLE [dbo].[PageTag] CHECK CONSTRAINT ALL
GO
DROP TABLE #tmp_a85dfe253b93442790a8553bae0a30fa
GO
