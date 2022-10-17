CREATE TABLE #tmp_bf9d20e4eb2b43ec8b70458a7a5f992d ([PageId] [int],[Tag] [nvarchar](128))
GO

INSERT INTO #tmp_bf9d20e4eb2b43ec8b70458a7a5f992d ([PageId],[Tag]) VALUES
(1,'Home'),
(1,'Official'),
(3,'Help'),
(3,'Official'),
(3,'Official-Help'),
(3,'Wiki'),
(22,'Help'),
(22,'Official'),
(22,'Official-Help'),
(22,'Wiki'),
(23,'Help'),
(23,'Official'),
(23,'Official-Help'),
(23,'Wiki'),
(24,'Help'),
(24,'Official'),
(24,'Official-Help'),
(24,'Wiki'),
(25,'Help'),
(25,'Official'),
(25,'Official-Help'),
(25,'Wiki'),
(26,'Help'),
(26,'Official'),
(26,'Official-Help'),
(26,'Wiki'),
(27,'Help'),
(27,'Official'),
(27,'Official-Help'),
(27,'Wiki'),
(28,'Help'),
(28,'Official'),
(28,'Official-Help'),
(28,'Wiki'),
(29,'Help'),
(29,'Official'),
(29,'Official-Help'),
(29,'Wiki'),
(30,'Help'),
(30,'Official'),
(30,'Official-Help'),
(30,'Wiki'),
(328,'Help'),
(328,'Official'),
(328,'Official-Help'),
(328,'Wiki'),
(330,'Help'),
(330,'Official'),
(330,'Official-Help'),
(330,'Wiki'),
(331,'Include'),
(332,'Help'),
(332,'Official'),
(332,'Official-Help'),
(332,'Wiki'),
(334,'Config'),
(335,'Draft'),
(336,'Help'),
(336,'Official'),
(336,'Official-Help'),
(336,'Wiki'),
(339,'Include'),
(345,'Help'),
(345,'Media'),
(345,'Official'),
(345,'Wiki'),
(346,'Help'),
(346,'Official'),
(346,'Official-Help'),
(346,'Wiki'),
(347,'Draft')
GO
ALTER TABLE [dbo].[PageTag] NOCHECK CONSTRAINT ALL
GO
UPDATE T SET
FROM [dbo].[PageTag] as T
INNER JOIN #tmp_bf9d20e4eb2b43ec8b70458a7a5f992d as S
ON T.[PageId] = S.[PageId] AND T.[Tag] = S.[Tag]
GO
INSERT INTO [dbo].[PageTag] (
	[PageId],[Tag])
SELECT
	[PageId],[Tag]
FROM #tmp_bf9d20e4eb2b43ec8b70458a7a5f992d as S
WHERE NOT EXISTS (
	SELECT TOP 1 1 FROM  [dbo].[PageTag] as T
	WHERE T.[PageId] = S.[PageId] AND T.[Tag] = S.[Tag]
)
ALTER TABLE [dbo].[PageTag] CHECK CONSTRAINT ALL
GO
DROP TABLE #tmp_bf9d20e4eb2b43ec8b70458a7a5f992d
GO
