SELECT
	E.Id,
	E.[Name],
	E.MimeType,
	'%%' || lower(E.[Name]) || '%%' as Shortcut,
	ImageData
FROM
	Emoji as E
WHERE
	E.[Name] = @Name
