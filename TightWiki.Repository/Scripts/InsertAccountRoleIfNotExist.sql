INSERT INTO AccountRole(UserId, RoleId)
SELECT @UserId, @RoleId
WHERE NOT EXISTS (
	SELECT 1 FROM AccountRole WHERE UserId = @UserId AND RoleId = @RoleId
);

SELECT
	AR.Id,
	P.UserId,
	P.Navigation,
	P.AccountName,
	U.Email as EmailAddress,
	(select UC.ClaimValue from AspNetUserClaims as UC WHERE UC.UserId = U.Id AND UC.ClaimType = 'firstname') as FirstName,
	(select UC.ClaimValue from AspNetUserClaims as UC WHERE UC.UserId = U.Id AND UC.ClaimType = 'lastname') as LastName
FROM
	Profile as P
INNER JOIN AspNetUsers as U
	ON U.Id = P.UserId
INNER JOIN AccountRole as AR
	ON AR.UserId = P.UserId
	AND AR.ID = last_insert_rowid()
ORDER BY
	P.AccountName
