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
	ANU.EmailConfirmed,
	@PageSize as PaginationSize,
	(
		SELECT
			Round(Count(0) / (@PageSize + 0.0)  + 0.999)
		FROM
			[Profile] as P
		INNER JOIN AspNetUserClaims as UCR
			ON UCR.UserId = U.UserId
			AND UCR.ClaimType LIKE '%/role'
		INNER JOIN Role as R
			ON R.Name = UCR.ClaimValue
		LEFT OUTER JOIN AspNetUserClaims as UCFirstName
			ON UCR.UserId = U.UserId
			AND UCR.ClaimType = 'firstname'
		LEFT OUTER JOIN AspNetUserClaims as UCLastName
			ON UCR.UserId = U.UserId
			AND UCR.ClaimType = 'lastname'			
		WHERE
			@SearchToken IS NULL
			OR U.AccountName LIKE '%' || @SearchToken || '%'
			OR ANU.Email LIKE '%' || @SearchToken || '%'
			OR UCFirstName.ClaimValue LIKE '%' || @SearchToken || '%'
			OR UCLastName.ClaimValue LIKE '%' || @SearchToken || '%'
	) as PaginationCount
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
WHERE
	@SearchToken IS NULL
	OR U.AccountName LIKE '%' || @SearchToken || '%'
	OR ANU.Email LIKE '%' || @SearchToken || '%'
	OR UCFirstName.ClaimValue LIKE '%' || @SearchToken || '%'
	OR UCLastName.ClaimValue LIKE '%' || @SearchToken || '%'
ORDER BY
	U.AccountName,
	U.UserId
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize
