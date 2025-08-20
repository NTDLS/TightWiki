SELECT
	AR.Id,
	R.Name,
	AR.RoleId,
	@PageSize as PaginationPageSize,
	(
		SELECT
			(Round(Count(0) / (@PageSize + 0.0) + 0.999))
		FROM
			Profile as U
		INNER JOIN AspNetUsers as ANU
			ON ANU.Id = U.UserId
		INNER JOIN AccountRole as AR
			ON AR.UserId = U.UserId
		INNER JOIN Role as R
			ON R.Id = AR.RoleId
		WHERE
			U.UserId = @UserId
	) as PaginationPageCount
FROM
	Profile as U
INNER JOIN AspNetUsers as ANU
	ON ANU.Id = U.UserId
INNER JOIN AccountRole as AR
	ON AR.UserId = U.UserId
INNER JOIN Role as R
	ON R.Id = AR.RoleId
WHERE
	U.UserId = @UserId
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
