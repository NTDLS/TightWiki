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
	@PageSize as PaginationPageSize,
	(
		SELECT
			CAST((Count(0) + (@PageSize - 1.0)) / @PageSize AS INTEGER)
		FROM
			Profile as P
		LEFT OUTER JOIN AspNetUserClaims as UCFirstName
			ON UCFirstName.UserId = U.UserId
			AND UCFirstName.ClaimType = 'firstname'
		LEFT OUTER JOIN AspNetUserClaims as UCLastName
			ON UCLastName.UserId = U.UserId
			AND UCLastName.ClaimType = 'lastname'			
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
LEFT OUTER JOIN AspNetUserClaims as UCFirstName
	ON UCFirstName.UserId = U.UserId
	AND UCFirstName.ClaimType = 'firstname'
LEFT OUTER JOIN AspNetUserClaims as UCLastName
	ON UCLastName.UserId = U.UserId
	AND UCLastName.ClaimType = 'lastname'
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
