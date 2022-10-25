IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[GetUsersByRoleId]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[GetUsersByRoleId] AS'
END
GO


ALTER PROCEDURE [dbo].[GetUsersByRoleId]
(
	@RoleId int,
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
		U.Id,
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
		U.LastLoginDate,
		U.EmailVerified,
		@PaginationSize as PaginationSize,
		(
			SELECT
				CEILING(Count(0) / (@PaginationSize + 0.0))
			FROM
				[User] as P
			INNER JOIN UserRole as UR
				ON UR.UserId = P.Id
			WHERE
				UR.RoleId = @RoleId
		) as PaginationCount
	FROM
		[User] as U
	INNER JOIN UserRole as UR
		ON UR.UserId = U.Id
	WHERE
		UR.RoleId = @RoleId
	ORDER BY
		U.AccountName,
		U.Id
	OFFSET ((@PageNumber - 1) * @PaginationSize) ROWS FETCH NEXT @PaginationSize ROWS ONLY

END--PROCEDURE