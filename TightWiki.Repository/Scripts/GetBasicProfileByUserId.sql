SELECT
	U.UserId,
	U.AccountName,
	U.Navigation,
	U.Biography,
	(select UC.ClaimValue from AspNetUserClaims as UC WHERE UC.UserId = U.UserId AND UC.ClaimType = 'theme') as Theme,
	UCR.ClaimValue as Role
FROM
	Profile as U
INNER JOIN AspNetUsers as ANU
	ON ANU.Id = U.UserId
INNER JOIN AspNetUserClaims as UCR
	ON UCR.UserId = U.UserId
	AND UCR.ClaimType LIKE '%/role'
INNER JOIN Role as R
	ON R.Name = UCR.ClaimValue
WHERE
	U.UserId = @UserId
