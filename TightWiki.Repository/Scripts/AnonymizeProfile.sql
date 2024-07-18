UPDATE
	Profile
SET
	AccountName = @StandinName,
	Navigation = @Navigation,
	Biography = 'Deleted account.',
	Avatar = null,
	ModifiedDate = @ModifiedDate
WHERE
	UserId = @UserId
