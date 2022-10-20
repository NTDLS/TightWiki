IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[GetAllPagesByInstructionPaged]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[GetAllPagesByInstructionPaged] AS'
END
GO

ALTER PROCEDURE [dbo].[GetAllPagesByInstructionPaged]
(
	@PageNumber int = 1,
	@PageSize int = 0,
	@Instruction nvarchar(128)
) AS
BEGIN--PROCEDURE
	SET NOCOUNT ON;

	DECLARE @PaginationSize int = @PageSize
	
	IF(@PageSize = 0)
	BEGIN--IF
		SELECT
			@PaginationSize = Cast(CE.[Value] as Int)
		FROM
			[ConfigurationEntry] as CE
		INNER JOIN [ConfigurationGroup] as CG
			ON CG.Id = CE.ConfigurationGroupId
		WHERE
			CG.[Name] = 'Basic'
			AND CE.[Name] = 'Pagination Size'
	END--IF

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
		@PaginationSize as PaginationSize,
		(
			SELECT
				CEILING(Count(0) / (@PaginationSize + 0.0))
			FROM
				[Page] as P
			INNER JOIN PageProcessingInstruction as PPI
				ON PPI.PageId = P.Id
			WHERE
				PPI.Instruction = @Instruction
		) as PaginationCount
	FROM
		[Page] as P
	INNER JOIN [User] as ModifiedUser
		ON ModifiedUser.Id = P.ModifiedByUserId
	INNER JOIN [User] as Createduser
		ON Createduser.Id = P.CreatedByUserId
	INNER JOIN PageProcessingInstruction as PPI
		ON PPI.PageId = P.Id
	WHERE
		PPI.Instruction = @Instruction
	ORDER BY
		P.[Name],
		P.Id
	OFFSET ((@PageNumber - 1) * @PaginationSize) ROWS FETCH NEXT @PaginationSize ROWS ONLY

END--PROCEDURE