SELECT
	[Avatar] as Bytes,
	AvatarContentType as ContentType
FROM
	Profile
WHERE
	Navigation = @Navigation
