SELECT
	PT.Tag,
	PT.Navigation
FROM
	[PageTag] as PT
WHERE
	PT.PageId = @PageId
