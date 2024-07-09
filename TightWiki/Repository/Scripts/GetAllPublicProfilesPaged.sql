--This proc is exactly like GetAllUsersPaged except it has no filter on personal infomation so it can be used for public info.

SELECT
	U.UserId,
	U.AccountName,
	U.Navigation,
	(select UC.ClaimValue from AspNetUserClaims as UC WHERE UC.UserId = U.UserId AND UC.ClaimType = 'timezone') as TimeZone,
	(select UC.ClaimValue from AspNetUserClaims as UC WHERE UC.UserId = U.UserId AND UC.ClaimType = 'language') as Language,
	(select UC.ClaimValue from AspNetUserClaims as UC WHERE UC.UserId = U.UserId AND UC.ClaimType LIKE '%/country') as Country,
	U.CreatedDate,
	U.ModifiedDate,
	UCR.ClaimValue as Role,
	@PageSize as PaginationPageSize,
	(
		SELECT
			CAST((Count(0) + (@PageSize - 1.0)) / @PageSize AS INTEGER)
		FROM
			Profile as P
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
	) as PaginationPageCount
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
