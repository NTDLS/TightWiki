SELECT
	U.UserId,
	U.AccountName,
	U.Navigation
FROM
	Profile as U
WHERE
	U.UserId = @UserId
