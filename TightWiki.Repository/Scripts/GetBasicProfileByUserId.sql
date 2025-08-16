SELECT
	U.UserId,
	U.AccountName,
	U.Navigation,
	U.Biography,
	(select UC.ClaimValue from AspNetUserClaims as UC WHERE UC.UserId = U.UserId AND UC.ClaimType = 'theme') as Theme
FROM
	Profile as U
INNER JOIN AspNetUsers as ANU
	ON ANU.Id = U.UserId
WHERE
	U.UserId = @UserId
