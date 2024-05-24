SELECT DISTINCT
	E.[Id],
	E.[Name],
	E.MimeType,
	'%%' || lower([Name]) || '%%' as [Shortcut]
FROM
	Emoji as E
INNER JOIN EmojiCategory as EC
	ON EC.EmojiId = E.Id
WHERE
	EC.Category = @Category
ORDER BY
	E.[Name]