CREATE TABLE #tmp_2c7f4cd04ac84dddbcaa1637564b6890 ([PageId] [int],[Tag] [nvarchar](128))
GO

INSERT INTO #tmp_2c7f4cd04ac84dddbcaa1637564b6890 ([PageId],[Tag]) VALUES
(1,'Canned'),
(1,'Default'),
(1,'Example'),
(1,'Home Page'),
(1,'McMan'),
(1,'NetworkDLS'),
(1,'Test'),
(1,'The FBI Raided'),
(1,'The Home Page'),
(3,'Draft')
GO
ALTER TABLE [dbo].[PageTag] NOCHECK CONSTRAINT ALL
GO
UPDATE T SET
FROM [dbo].[PageTag] as T
INNER JOIN #tmp_2c7f4cd04ac84dddbcaa1637564b6890 as S
ON T.[PageId] = S.[PageId] AND T.[Tag] = S.[Tag]
GO
INSERT INTO [dbo].[PageTag] (
	[PageId],[Tag])
SELECT
	[PageId],[Tag]
FROM #tmp_2c7f4cd04ac84dddbcaa1637564b6890 as S
WHERE NOT EXISTS (
	SELECT TOP 1 1 FROM  [dbo].[PageTag] as T
	WHERE T.[PageId] = S.[PageId] AND T.[Tag] = S.[Tag]
)
ALTER TABLE [dbo].[PageTag] CHECK CONSTRAINT ALL
GO
DROP TABLE #tmp_2c7f4cd04ac84dddbcaa1637564b6890
GO
