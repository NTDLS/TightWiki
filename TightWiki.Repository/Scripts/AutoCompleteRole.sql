SELECT
	R.Id,
	R.[Name]
FROM
	Role as R
WHERE
	R.[Name] LIKE '%' || @SearchText || '%'
ORDER BY
	R.[Name]
LIMIT 25;

