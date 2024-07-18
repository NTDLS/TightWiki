SELECT
	P.Id,
	P.[Name],
	P.Navigation,
	P.[Description],
	P.Revision,
	P.CreatedByUserId,
	P.CreatedDate,
	P.ModifiedByUserId,
	P.ModifiedDate,
	Createduser.AccountName as CreatedByUserName,
	ModifiedUser.AccountName as ModifiedByUserName,
	@PageSize as PaginationPageSize,
	(
		SELECT
			CAST((Count(0) + (@PageSize - 1.0)) / @PageSize AS INTEGER)
		FROM
			[Page] as P
		INNER JOIN PageProcessingInstruction as PPI
			ON PPI.PageId = P.Id
		WHERE
			PPI.Instruction = @Instruction
	) as PaginationPageCount
FROM
	[Page] as P
INNER JOIN users_db.Profile as ModifiedUser
	ON ModifiedUser.UserId = P.ModifiedByUserId
INNER JOIN users_db.Profile as Createduser
	ON Createduser.UserId = P.CreatedByUserId
INNER JOIN PageProcessingInstruction as PPI
	ON PPI.PageId = P.Id
WHERE
	PPI.Instruction = @Instruction
ORDER BY
	P.[Name],
	P.Id
LIMIT @PageSize
OFFSET (@PageNumber - 1) * @PageSize
