UPDATE
	Profile
SET
	[AccountName] = @AccountName,
	[Navigation] = @Navigation,
	[Biography] = @Biography
WHERE
	UserId = @UserId
