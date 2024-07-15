SELECT
	Count(0)
FROM
	[PageRevision] as PR
INNER JOIN Page as P
	ON P.Id = PR.PageId
WHERE
	P.Navigation = @Navigation
