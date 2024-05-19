SELECT
	Category,
	(
		SELECT
			Count(0)
		FROM
			EmojiCategory as iEC
		INNER JOIN Emoji as iE
			ON iE.Id = iEC.EmojiId
		WHERE
			iEC.Category = EC.Category
	) as EmojiCount
FROM
	EmojiCategory as EC
INNER JOIN Emoji as E
	ON E.Id = EC.EmojiId
WHERE
	E.[Name] NOT LIKE '%' || EC.Category || '%'
GROUP BY
	Category
ORDER BY
	Category