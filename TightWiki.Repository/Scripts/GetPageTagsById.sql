SELECT
	PT.Tag
FROM
	[PageTag] as PT
WHERE
	PT.PageId = @PageId
