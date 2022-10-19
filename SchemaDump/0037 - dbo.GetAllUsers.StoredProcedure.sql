IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[GetAllUsers]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[GetAllUsers] AS'
END
GO



ALTER PROCEDURE [dbo].[GetAllUsers]
(
	@PageNumber int = 1,
	@PageSize int = 0
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
		Id,
		EmailAddress,
		AccountName,
		Navigation,
		PasswordHash,
		FirstName,
		LastName,
		TimeZone,
		Country,
		CreatedDate,
		ModifiedDate,
		LastLoginDate,
		EmailVerified,
		@PaginationSize as PaginationSize,
		(
			SELECT
				CEILING(Count(0) / (@PaginationSize + 0.0))
			FROM
				[User] as P
		) as PaginationCount
	FROM
		[User]
	ORDER BY
		AccountName,
		Id
	OFFSET ((@PageNumber - 1) * @PaginationSize) ROWS FETCH NEXT @PaginationSize ROWS ONLY

END--PROCEDURE