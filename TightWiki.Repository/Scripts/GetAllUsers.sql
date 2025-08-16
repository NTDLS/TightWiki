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
	ANU.EmailConfirmed
FROM
	Profile as U
INNER JOIN AspNetUsers as ANU
	ON ANU.Id = U.UserId
LEFT OUTER JOIN AspNetUserClaims as UCFirstName
	ON UCFirstName.UserId = U.UserId
	AND UCFirstName.ClaimType = 'firstname'
LEFT OUTER JOIN AspNetUserClaims as UCLastName
	ON UCLastName.UserId = U.UserId
	AND UCLastName.ClaimType = 'lastname'
