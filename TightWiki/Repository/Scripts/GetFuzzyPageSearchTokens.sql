SELECT
	PageId,
	SUM([Match]) as [Match],
	SUM([Weight]) as [Weight],
	SUM([Score]) as [Score]
FROM
(
	SELECT
		T.PageId,
		COUNT(DISTINCT T.DoubleMetaphone) / (@TokenCount + 0.0) as [Match],
		SUM(T.[Weight] * 1.0) as [Weight],
		--No weight benefits on score for fuzzy matching weight for exact matches:
		(COUNT(DISTINCT T.DoubleMetaphone) / (@TokenCount + 0.0)) as [Score]
	FROM
		PageToken as T
	INNER JOIN TempSearchTerms as ST
		ON ST.Token != T.Token
		AND ST.DoubleMetaphone = T.DoubleMetaphone
	GROUP BY
		T.PageId
	) as T
GROUP BY
	T.PageId
HAVING
	SUM(Score) >= @MinimumMatchScore
ORDER BY
	SUM([Score]) DESC
LIMIT 250;
