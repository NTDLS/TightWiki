INSERT INTO ApiTokens
(
	UserId,
	Name,
	TokenHash,
	CreatedDate
)
SELECT
	@UserId,
	@Name,
	@TokenHash,
	@CreatedDate
