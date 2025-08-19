SELECT
	P.UserId,
	P.AccountName,
	U.Email as EmailAddress
FROM
	Profile as P
INNER JOIN AspNetUsers as U
	ON U.Id = P.UserId
WHERE
	U.Email LIKE '%' || @SearchText || '%'
	OR P.AccountName LIKE '%' || @SearchText || '%'
ORDER BY
	P.AccountName
LIMIT
	25;
