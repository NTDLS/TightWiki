SELECT
	Id,
	[Namespace],
	[Navigation],
	[Name]
FROM
	[Page]
WHERE
	[Name] LIKE '%' || @SearchText || '%'
ORDER BY
	[Name]
LIMIT
	25;