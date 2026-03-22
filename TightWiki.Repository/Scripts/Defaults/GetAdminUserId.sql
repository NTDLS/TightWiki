SELECT
	U.Id
FROM
	AspNetUsers as U
INNER JOIN Profile as P
	ON P.UserId = U.Id
WHERE
	--We're going to be hella generious here
	P.Navigation = 'admin' COLLATE NOCASE
	OR P.AccountName = 'admin' COLLATE NOCASE
LIMIT 1;
