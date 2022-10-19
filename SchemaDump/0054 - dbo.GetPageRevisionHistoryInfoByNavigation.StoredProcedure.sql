IF NOT EXISTS(SELECT TOP 1 1 FROM sys.objects WHERE object_id = object_id('[dbo].[GetPageRevisionHistoryInfoByNavigation]'))
BEGIN
    EXEC dbo.sp_executesql @statement = N'CREATE PROCEDURE [dbo].[GetPageRevisionHistoryInfoByNavigation] AS'
END
GO



ALTER PROCEDURE [dbo].[GetPageRevisionHistoryInfoByNavigation]
(
	@Navigation nvarchar(128),
	@PageNumber int = 0,
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
		P.Id as PageId,
		PR.[Name],
		PR.[Description],
		PR.Revision,
		P.Navigation,
		P.CreatedByUserId,
		Createduser.AccountName as CreatedByUserName,
		P.CreatedDate,
		PR.ModifiedByUserId,
		ModifiedUser.AccountName as ModifiedByUserName,
		PR.ModifiedDate,
		@PaginationSize as PaginationSize,
		(
			SELECT
				CEILING(Count(0) / (@PaginationSize + 0.0))
			FROM
				[Page] as P
			INNER JOIN [PageRevision] as PR
				ON PR.PageId = P.Id
			WHERE
				P.Navigation = @Navigation
		) as PaginationCount
	FROM
		[Page] as P
	INNER JOIN [PageRevision] as PR
		ON PR.PageId = P.Id
	INNER JOIN [User] as ModifiedUser
		ON ModifiedUser.Id = PR.ModifiedByUserId
	INNER JOIN [User] as Createduser
		ON Createduser.Id = P.CreatedByUserId
	WHERE
		P.Navigation = @Navigation
	ORDER BY
		PR.Revision DESC
	OFFSET ((@PageNumber - 1) * @PaginationSize) ROWS FETCH NEXT @PaginationSize ROWS ONLY

END--PROCEDURE