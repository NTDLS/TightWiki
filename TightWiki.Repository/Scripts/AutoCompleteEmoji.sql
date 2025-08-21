SELECT
	E.Name
FROM
	Emoji as E
WHERE
	Name LIKE '%' || @Term || '%'
	AND EXISTS (	
		SELECT
			EmojiId FROM EmojiCategory as EC
		WHERE
			EC.EmojiId = E.Id
			AND EC.Category LIKE '%' || @Term || '%'
	)
ORDER BY
	E.Name
LIMIT 25;
