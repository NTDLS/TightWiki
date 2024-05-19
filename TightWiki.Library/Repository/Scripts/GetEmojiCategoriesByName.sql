SELECT
	Ec.Id,
	EC.EmojiId,
	EC.Category
FROM
	Emoji as E
INNER JOIN EmojiCategory as EC
	ON EC.EmojiId = E.Id
WHERE
	E.[Name] = @Name
