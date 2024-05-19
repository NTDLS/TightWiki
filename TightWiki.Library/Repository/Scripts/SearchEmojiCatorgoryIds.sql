SELECT
	EC.Id
FROM
	TempCategories as SS
INNER JOIN EmojiCategory as EC
	ON EC.Category LIKE SS.[value] || '%'
GROUP BY
	EmojiId
HAVING
	COUNT(0) >= @SearchTokenCount
