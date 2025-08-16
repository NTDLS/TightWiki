SELECT
	U.UserId,
	ANU.Email as EmailAddress,
	U.AccountName,
	U.Navigation,
	(select UC.ClaimValue from AspNetUserClaims as UC WHERE UC.UserId = U.UserId AND UC.ClaimType = 'firstname') as FirstName,
	(select UC.ClaimValue from AspNetUserClaims as UC WHERE UC.UserId = U.UserId AND UC.ClaimType = 'lastname') as LastName,
	(select UC.ClaimValue from AspNetUserClaims as UC WHERE UC.UserId = U.UserId AND UC.ClaimType = 'timezone') as TimeZone,
	(select UC.ClaimValue from AspNetUserClaims as UC WHERE UC.UserId = U.UserId AND UC.ClaimType = 'language') as Language,
	(select UC.ClaimValue from AspNetUserClaims as UC WHERE UC.UserId = U.UserId AND UC.ClaimType LIKE '%/country') as Country,
	U.CreatedDate,
	U.ModifiedDate,
	UCR.ClaimValue as Role,
	ANU.EmailConfirmed,
	@PageSize as PaginationPageSize,
	(
		SELECT
			CAST((Count(0) + (@PageSize - 1.0)) / @PageSize AS INTEGER)
		FROM
			Profile as P
		INNER JOIN AspNetUserClaims as UCR
			ON UCR.UserId = U.UserId
			AND UCR.ClaimType LIKE '%/role' -- TODO: This is no longer supported by TightWiki.
		INNER JOIN Role as R
			ON R.Name = UCR.ClaimValue
		WHERE
			R.Id = @RoleId
	) as PaginationPageCount
FROM
	Profile as U
INNER JOIN AspNetUsers as ANU
	ON ANU.Id = U.UserId
INNER JOIN AspNetUserClaims as UCR
	ON UCR.UserId = U.UserId
	AND UCR.ClaimType LIKE '%/role' -- TODO: This is no longer supported by TightWiki.
INNER JOIN Role as R
	ON R.Name = UCR.ClaimValue
WHERE
	R.Id = @RoleId
ORDER BY
	U.AccountName,
	U.UserId
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize
