--This proc is exactly like GetAllUsersPaged except it has no filter on personal infomation so it can be used for public info.

SELECT
	U.UserId,
	U.EmailAddress,
	U.AccountName,
	U.Navigation,
	U.PasswordHash,
	U.FirstName,
	U.LastName,
	U.TimeZone,
	U.[Language],
	U.Country,
	U.CreatedDate,
	U.ModifiedDate,
	U.EmailConfirmed,
	R.[Name] as [Role],
	@PageSize as PaginationSize,
	(
		SELECT
			Round(Count(0) / (@PageSize + 0.0)  + 0.999)
		FROM
			[User] as P
		WHERE
			@SearchToken IS NULL
			OR AccountName LIKE '%' || @SearchToken || '%'
	) as PaginationCount
FROM
	Profile as U
INNER JOIN [Role] as R
	ON R.Id = U.RoleId
WHERE
	U.Deleted = 0
	AND @SearchToken IS NULL
	OR U.AccountName LIKE '%' || @SearchToken || '%'
ORDER BY
	U.AccountName,
	U.UserId
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize
