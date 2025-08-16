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
	ANU.EmailConfirmed,
	@PageSize as PaginationPageSize,
	(
		SELECT
			CAST((Count(0) + (@PageSize - 1.0)) / @PageSize AS INTEGER)
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
--CUSTOM_ORDER_BEGIN::
--CONFIG::
/*
Account=U.AccountName
FirstName=UCFirstName.ClaimValue
LastName=UCLastName.ClaimValue
Created=U.CreatedDate
TimeZone=TimeZone
Language=Language
Country=Country
EmailAddress=ANU.Email
*/
--::CONFIG
ORDER BY
	U.AccountName
--::CUSTOM_ORDER_BEGIN
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize
