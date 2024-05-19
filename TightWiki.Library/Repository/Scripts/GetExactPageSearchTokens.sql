SELECT
	PageId,
	SUM([Match]) as [Match],
	SUM([Weight]) as [Weight],
	SUM([Score]) as [Score]
FROM
(
	SELECT
		T.PageId,
		COUNT(DISTINCT T.Token) / (@TokenCount + 0.0) as [Match],
		SUM(T.[Weight] * 1.5) as [Weight],
		--Extra weight on score for exact matches:
		SUM(T.[Weight] * 1.5) * (COUNT(DISTINCT T.Token) / (@TokenCount + 0.0)) as [Score]
	FROM
		PageToken as T
	INNER JOIN TempSearchTerms as ST
		ON ST.Token = T.Token
	GROUP BY
		T.PageId
	) as T
GROUP BY
	T.PageId
HAVING
	SUM(Score) >= @MinimumMatchScore
