SELECT
	MAX([Extent].Tag) as Tag,
	[Extent].Navigation,
	Count(DISTINCT [Extent].PageId) as [PageCount]
FROM
	PageTag as [Root]
INNER JOIN PageTag as [Interm]
	ON [Interm].Navigation = [Root].Navigation
	AND [Interm].[PageId] = [Root].[PageId]
INNER JOIN PageTag as [Extent]
	ON [Extent].[PageId] = [Interm].[PageId]
WHERE
	[Root].Navigation = @Tag
GROUP BY
	[Extent].Navigation
LIMIT 100;
