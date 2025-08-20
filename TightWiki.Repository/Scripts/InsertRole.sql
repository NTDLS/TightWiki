INSERT INTO [Role]
(
	[Name],
	[Description],
	IsBuiltIn
)
SELECT
	@Name,
	@Description,
	0
