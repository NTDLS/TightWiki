SELECT
	[PageId],
	[Token],
	[DoubleMetaphone],
	[Weight]
FROM
	[PageToken]
WHERE
	[PageId] = @PageId
