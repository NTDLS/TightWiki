SELECT
	Id,
	[Name],
	[Description],
	IsBuiltIn
FROM
	[Role]
WHERE
	[Name] = @Name
