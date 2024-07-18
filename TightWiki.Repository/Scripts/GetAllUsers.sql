SELECT
	U.UserId,
	ANU.Email as EmailAddress,
	U.AccountName,
	U.Navigation,
	UCFirstName.ClaimValue as FirstName,
	UCLastName.ClaimValue as LastName,
	(select UC.ClaimValue from AspNetUserClaims as UC WHERE UC.UserId = U.UserId AND UC.ClaimType = 'timezone') as TimeZone,
	(select UC.ClaimValue from AspNetUserClaims as UC WHERE UC.UserId = U.UserId AND UC.ClaimType = 'language') as Language,
	(select UC.ClaimValue from AspNetUserClaims as UC WHERE UC.UserId = U.UserId AND UC.ClaimType LIKE '%/country') as Country,
	U.CreatedDate,
	U.ModifiedDate,
	UCR.ClaimValue as Role,
	ANU.EmailConfirmed
FROM
	Profile as U
INNER JOIN AspNetUsers as ANU
	ON ANU.Id = U.UserId
INNER JOIN AspNetUserClaims as UCR
	ON UCR.UserId = U.UserId
	AND UCR.ClaimType LIKE '%/role'
LEFT OUTER JOIN AspNetUserClaims as UCFirstName
	ON UCR.UserId = U.UserId
	AND UCR.ClaimType = 'firstname'
LEFT OUTER JOIN AspNetUserClaims as UCLastName
	ON UCR.UserId = U.UserId
	AND UCR.ClaimType = 'lastname'
INNER JOIN Role as R
	ON R.Name = UCR.ClaimValue
