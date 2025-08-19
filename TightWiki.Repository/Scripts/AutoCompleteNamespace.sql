SELECT DISTINCT
	[Namespace]
FROM
	[Page]
WHERE
	[Namespace] LIKE '%' || @SearchText || '%'
ORDER BY
	[Name]
LIMIT
	25;
