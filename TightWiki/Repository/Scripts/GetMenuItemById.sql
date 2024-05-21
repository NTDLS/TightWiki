SELECT
	[Id] as [Id],
	[Name] as [Name],
	[Link] as [Link],
	[Ordinal] as [Ordinal]
FROM
	[MenuItem]
WHERE
	Id = @Id
ORDER BY
	[Ordinal]