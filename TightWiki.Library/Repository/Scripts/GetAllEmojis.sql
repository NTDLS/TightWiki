SELECT
	[Id],
	[Name],
	MimeType,
	'%%' || lower([Name]) || '%%' as [Shortcut]
FROM
	Emoji
ORDER BY
	[Name]
