IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[GetAllPages]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[GetAllPages] AS'
END
GO



ALTER PROCEDURE [dbo].[GetAllPages]
(
	@PageNumber int = 1,
	@PageSize int = 0,
	@SearchTerms nvarchar(MAX) = null
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

	DECLARE @PageIDs TABLE
	(
		Id INT
	)

	DECLARE @Tokens INT = (SELECT COUNT(0) FROM STRING_SPLIT(@SearchTerms ,','))

	INSERT INTO @PageIDs
	(
		Id
	)
	SELECT
		T.PageId
	FROM
		PageToken as T
	INNER JOIN STRING_SPLIT(@SearchTerms ,',') as ST
		ON ST.[value] = T.Token
	WHERE
		IsNull(ST.[value], '') <> ''
	GROUP BY
		T.PageId
	HAVING
		Count(0) = @Tokens

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
			WHERE
				(IsNull(@SearchTerms, '') = ''
					OR P.Id IN (SELECT PID.Id FROM @PageIDs as PID)
		)
		) as PaginationCount
	FROM
		[Page] as P
	INNER JOIN [User] as ModifiedUser
		ON ModifiedUser.Id = P.ModifiedByUserId
	INNER JOIN [User] as Createduser
		ON Createduser.Id = P.CreatedByUserId
	WHERE
		(IsNull(@SearchTerms, '') = ''
			OR P.Id IN (SELECT PID.Id FROM @PageIDs as PID)
		)
	ORDER BY
		P.[Name],
		P.Id
	OFFSET ((@PageNumber - 1) * @PaginationSize) ROWS FETCH NEXT @PaginationSize ROWS ONLY

END--PROCEDURE