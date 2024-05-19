SELECT
	T.PageId
FROM
	PageToken as T
INNER JOIN TempTokens as TT
	ON TT.[value] = T.Token
WHERE
	Coalesce(TT.[value], '') <> ''
GROUP BY
	T.PageId
HAVING
	Count(0) = @TokenCount
