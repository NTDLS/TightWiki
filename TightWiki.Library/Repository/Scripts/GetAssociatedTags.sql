SELECT
	[Extent].[Tag],
	Count(DISTINCT [Extent].PageId) as [PageCount]
FROM
	PageTag as [Root]
INNER JOIN PageTag as [Interm]
	ON [Interm].[Tag] = [Root].[Tag]
	AND [Interm].[PageId] = [Root].[PageId]
INNER JOIN PageTag as [Extent]
	ON [Extent].[PageId] = [Interm].[PageId]
WHERE
	[Root].[Tag] = @Tag
GROUP BY
	[Extent].[Tag]
LIMIT 100;
