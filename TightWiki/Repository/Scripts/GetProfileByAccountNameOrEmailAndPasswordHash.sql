SELECT
	U.UserId,
	U.EmailAddress,
	U.AccountName,
	U.Navigation,
	U.PasswordHash,
	U.FirstName,
	U.LastName,
	U.TimeZone,
	U.Country,
	U.[Language],
	U.Biography,
	U.CreatedDate,
	U.ModifiedDate,
	U.VerificationCode,
	R.[Name] as [Role],
	U.EmailConfirmed
FROM
	Profile as U
INNER JOIN [Role] as R
	 ON R.Id = U.RoleId
WHERE
	(U.EmailAddress = @AccountNameOrEmail OR U.AccountName = @AccountNameOrEmail)
	AND U.PasswordHash = @PasswordHash
