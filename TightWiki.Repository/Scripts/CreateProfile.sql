INSERT INTO Profile
(
	UserId,
	AccountName,
	Navigation,
	CreatedDate,
	ModifiedDate
)
SELECT
	@UserId,
	@AccountName,
	@Navigation,
	@CreatedDate,
	@ModifiedDate

