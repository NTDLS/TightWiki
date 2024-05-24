SELECT
	E.Id,
	E.[Name],
	E.MimeType,
	'%%' || lower(E.[Name]) || '%%' as Shortcut,
	@PageSize as PaginationPageSize,
	(
		SELECT
			(Round(Count(0) / (@PageSize + 0.0) + 0.999))
		FROM
			Emoji as iE
		INNER JOIN EmojiCategory as iEC
			ON iEC.EmojiId = iE.Id
		WHERE
			iEC.Id IN (SELECT Value FROM TempEmojiCategoryIds)
	) as PaginationPageCount
FROM
	Emoji as E
INNER JOIN EmojiCategory as EC
	ON EC.EmojiId = E.Id
WHERE
	EC.Id IN (SELECT Value FROM TempEmojiCategoryIds)
ORDER BY
	E.[Name]
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize
