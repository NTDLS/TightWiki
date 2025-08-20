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
	ANU.EmailConfirmed,
	@PageSize as PaginationPageSize,
	(
		SELECT
			(Round(Count(0) / (@PageSize + 0.0) + 0.999))
		FROM
			Profile as U
		INNER JOIN AspNetUsers as ANU
			ON ANU.Id = U.UserId
		INNER JOIN AccountRole as R
			ON R.UserId = U.UserId
		WHERE
			R.RoleId = @RoleId
	) as PaginationPageCount
FROM
	Profile as U
INNER JOIN AspNetUsers as ANU
	ON ANU.Id = U.UserId
INNER JOIN AccountRole as R
	ON R.UserId = U.UserId
WHERE
	R.RoleId = @RoleId
--CUSTOM_ORDER_BEGIN::
--CONFIG::
/*
EmailAddress=ANU.Email
AccountName=U.AccountName
LastName=(select UC.ClaimValue from AspNetUserClaims as UC WHERE UC.UserId = U.UserId AND UC.ClaimType = 'lastname')
FirstName=(select UC.ClaimValue from AspNetUserClaims as UC WHERE UC.UserId = U.UserId AND UC.ClaimType = 'firstname')
*/
--::CONFIG
ORDER BY
	U.AccountName,
	U.UserId
--::CUSTOM_ORDER_BEGIN
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize
