SELECT
	E.Id,
	E.[Name],
	E.MimeType,
	'%%' || lower(E.[Name]) || '%%' as Shortcut,
	@PageSize as PaginationPageSize,
	(
		SELECT
			(Count(0) + (@PageSize - 1)) / @PageSize
		FROM
			Emoji as iE
	) as PaginationPageCount
FROM
	Emoji as E
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
