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
--CUSTOM_ORDER_BEGIN::
--CONFIG::
/*
Name=E.[Name]
MimeType=E.[MimeType]
Shortcut=E.[Name]
*/
--::CONFIG
ORDER BY
	E.[Name]
--::CUSTOM_ORDER_BEGIN
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize
